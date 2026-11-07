using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;

using ST.Api.Configuration;
using ST.Api.Extensions;
using ST.Api.Features.Shared.Realtime;
using ST.Api.Health;
using ST.Api.Utilities;
using ST.Application.Extensions;
using ST.Infrastructure.Extensions;
using ST.Infrastructure.Features.IAM.Extensions;
using ST.Infrastructure.Features.IAM.Users.Seeding;
using ST.Infrastructure.Middleware;
using ST.Persistence.Features.ControlPlane.Extensions;
using ST.Persistence.Features.Shared.Extensions;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ST.Api.Middleware;
using ST.Infrastructure.Logging;

// ── Bootstrap logger ─────────────────────────────────────────────────────────
// Captures startup errors (before the host and appsettings are loaded).
// Replaced by the fully-configured logger once UseSerilog() runs below.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);
    ConfigurePlatformAssignedPort(builder.WebHost);

    // ── Key Vault — production secrets ────────────────────────────────────────
    // Only runs outside Development — User Secrets handle dev.
    // DefaultAzureCredential tries, in order:
    //   1. Environment variables (AZURE_CLIENT_ID etc.) — CI/CD pipelines
    //   2. Workload Identity — AKS pods
    //   3. Managed Identity — Azure App Service / Azure VM (zero config needed)
    //   4. Visual Studio credential
    //   5. Azure CLI credential — works on your machine after `az login`
    // In production on Azure App Service with Managed Identity enabled,
    // option 3 fires automatically — no credentials stored anywhere.
    if (!builder.Environment.IsDevelopment())
    {
        var keyVaultUri = new Uri(
            builder.Configuration["KeyVault:Uri"]
            ?? throw new InvalidOperationException("KeyVault:Uri is not configured."));

        builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
    }

    // ── Serilog — THE only place it is configured ─────────────────────────────
    // ReadFrom.Configuration reads appsettings.json + appsettings.{Environment}.json.
    // ReadFrom.Services allows enrichers that need DI (e.g. IHttpContextAccessor).
    // Do NOT call services.AddLogging(...AddSerilog) anywhere else — doing so
    // creates a second logger instance and loses enrichers set here.
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Destructure.With<PiiDestructuringPolicy>()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName);

        var otlpEndpoint = context.Configuration["Observability:Otlp:Endpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            var headerString = context.Configuration["Observability:Otlp:Headers"];
            var headers = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(headerString))
            {
                var pairs = headerString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var pair in pairs)
                {
                    var parts = pair.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        headers[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }

            configuration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = otlpEndpoint;
                options.Headers = headers;
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = context.Configuration["Observability:ServiceName"] ?? "StarterTemplate.API"
                };
            });
        }
    });

    // ── Services ─────────────────────────────────────────────────────────────
    builder.Services.AddApiServices(builder.Configuration, builder.Environment);
    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddSharedInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddIamModule(builder.Configuration, builder.Environment);
    builder.Services.AddSharedPersistence(builder.Configuration, builder.Environment);
    builder.Services.AddControlPlanePersistence(builder.Configuration, builder.Environment);
    builder.Services.ConfigureOutBoxMessagingWithGlobalRetry(builder.Configuration);

    builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                // Add error handling for problematic types
                options.JsonSerializerOptions.IgnoreReadOnlyProperties = false;
                options.JsonSerializerOptions.IncludeFields = false;

            });

    string corsPolicy = "ApiCorsPolicy";

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicy, policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

            if (allowedOrigins == null || allowedOrigins.Length == 0)
            {
                Log.Warning("No allowed origins configured for CORS policy");
                throw new InvalidOperationException("CORS policy requires at least one allowed origin to be configured.");
            }

            if (builder.Environment.IsDevelopment())
            {
                // More permissive in development
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetIsOriginAllowedToAllowWildcardSubdomains()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            }
            else
            {
                // Strict in production
                policy.WithOrigins(allowedOrigins)
                      .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
                      .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                      .AllowCredentials();
            }
        });
    });

    builder.Services.AddOpenApi("v1", options =>  // Note: "v1" to bypass interceptor
    {
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            // Remove parameters with null schema
            foreach (var param in operation.Parameters?.ToList() ?? [])
            {
                if (param.Schema == null)
                    operation.Parameters?.Remove(param);
            }

            // Remove parameter descriptions with null metadata
            for (int i = context.Description.ParameterDescriptions.Count - 1; i >= 0; i--)
            {
                if (context.Description.ParameterDescriptions[i].ModelMetadata == null)
                    context.Description.ParameterDescriptions.RemoveAt(i);
            }

            return Task.CompletedTask;
        });

        options.AddDocumentTransformer((document, context, _) =>
        {
            document.Info = new()
            {
                Title = "StarterTemplate API",
                Version = "v1.0",
                Description = """
                    StarterTemplate API

                    This API provides a production-ready foundation for:
                    - Authentication and authorization
                    - User and profile management
                    - Domain-driven module extension
                    - Documented, versioned REST endpoints
                    - Secure configuration and operational monitoring
                    """,
                Contact = new()
                {
                    Name = "StarterTemplate Support",
                    Email = "support@startertemplate.local",
                }
            };

            // ==========================================
            // GLOBAL AUTHENTICATION REQUIREMENT
            // ==========================================
            var schemeName = "Bearer";

            // Declare the scheme in components.securitySchemes
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes.Add(schemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your valid JWT token to authenticate globally."
            });

            // Reference it from the document-level `security` field => applies to ALL operations
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(schemeName, document)] = []
            });

            return Task.CompletedTask;
        });

        options.AddSchemaTransformer<SafeSchemaTransformer>();
    });

    builder.Services.Configure<HealthCheckSettings>(
        builder.Configuration.GetSection(HealthCheckSettings.SectionName));

    var healthCheckSettings = builder.Configuration
        .GetSection(HealthCheckSettings.SectionName)
        .Get<HealthCheckSettings>() ?? new HealthCheckSettings();

    var healthChecks = builder.Services.AddHealthChecks()
        .AddCheck("self-ready", () => HealthCheckResult.Healthy(), tags: ["ready"])
        .AddCheck("self-live", () => HealthCheckResult.Healthy(), tags: ["live"]);

    if (healthCheckSettings.SqlServer)
    {
        healthChecks.AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready", "dependency"]);
    }

    if (healthCheckSettings.Redis)
    {
        healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready", "dependency"]);
    }

    if (healthCheckSettings.ProfileImageStorage)
    {
        healthChecks.AddCheck<ProfileImageStorageHealthCheck>("profile-image-storage", tags: ["ready", "dependency"]);
    }

    if (healthCheckSettings.KeyVault)
    {
        healthChecks.AddCheck<KeyVaultHealthCheck>("key-vault", tags: ["ready", "dependency"]);
    }

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    var app = builder.Build();

    await app.SeedDevelopmentIdentityAsync().ConfigureAwait(false);

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler();
        app.UseHsts();
    }

    app.UseForwardedHeaders();

    app.UseInfrastructureLoggingMiddleware();

    // OpenAPI/Scalar mappings are configured below within the Development-only block.
    // The earlier unconditional mapping caused duplicate endpoints when the detailed
    // development mappings (with CacheOutput/AllowAnonymous) are also registered.
    // Keep a single MapOpenApi registration (see the Development block further down).

    app.UseSecurityHeaders();

    app.UseHttpsRedirection();

    var responseCompressionSettings = builder.Configuration
        .GetSection(ResponseCompressionSettings.SectionName)
        .Get<ResponseCompressionSettings>() ?? new ResponseCompressionSettings();

    if (responseCompressionSettings.Enabled)
    {
        app.UseResponseCompression();
    }

    app.UseRouting();

    app.UseMiddleware<TenantResolutionMiddleware>();

    app.UseOutputCache();

    app.UseCors(corsPolicy);

    app.UseRateLimiter();

    //app.UsePreAuthMiddleware();

    app.UseAuthentication();
    app.UseMiddleware<DemoWorkspaceMiddleware>();
    app.UseMiddleware<SupportSessionMiddleware>();

    app.UseMiddleware<ModuleAvailabilityMiddleware>();
    app.UsePostAuthMiddleware();

    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi()
           .CacheOutput()
           .AllowAnonymous();

        // Preferred way (Scalar.AspNetCore ≥ 1.x / 2.x)
        //app.MapScalarApiReference("/docs", options =>
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("StarterTemplate API")
                .WithTheme(ScalarTheme.Kepler)
                .AddPreferredSecuritySchemes("Bearer");   // or WithPreferredScheme for older
        });

        // Optional: redirect root to Scalar
        app.MapGet("/", () => Results.Redirect("/scalar/v1"))
        //app.MapGet("/", () => Results.Redirect("/docs"))
           .ExcludeFromDescription()
           .AllowAnonymous();
    }

    // General health check endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds
                }),

                totalDuration = report.TotalDuration.TotalMilliseconds
            };
            await context.Response.WriteAsJsonAsync(response).ConfigureAwait(false);
        }
    });

    // Detailed health check (for monitoring tools)
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });

    app.MapControllers();
    app.MapHub<NotificationHub>("/hubs/notifications");

    Log.Information("ST API started successfully");

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown intentionally during EF migration tooling runs —
    // do not log it as a fatal crash.
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
    throw;
}
finally
{
    // Flush and close all Serilog sinks before the process exits.
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}


static void ConfigurePlatformAssignedPort(ConfigureWebHostBuilder webHost)
{
    var port = Environment.GetEnvironmentVariable("PORT");

    if (string.IsNullOrWhiteSpace(port))
    {
        return;
    }

    if (!int.TryParse(port, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort) ||
        parsedPort <= 0)
    {
        throw new InvalidOperationException("The PORT environment variable must be a positive integer.");
    }

    webHost.UseUrls($"http://+:{parsedPort}");
}

public partial class Program { }

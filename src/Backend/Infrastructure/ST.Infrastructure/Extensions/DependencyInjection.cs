using System.Security.Claims;
using ST.Persistence.Common.Configuration;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.FeatureFlags.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Application.Features.Shared.Payments.Contracts.Interfaces;
using ST.Application.Features.Shared.Reporting.Contracts.Interfaces;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Settings;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Contracts.Implementations.Common;
using ST.Infrastructure.Contracts.Implementations.Settings;
using ST.Infrastructure.Contracts.Implementations.Caching;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;
using ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Storage;
using ST.Infrastructure.Features.Shared.FeatureFlags.Contracts.Implementations;
using ST.Infrastructure.Features.Shared.Notifications.Contracts.Implementations.Services;
using ST.Infrastructure.Features.Shared.Payments.Contracts.Implementations;
using ST.Infrastructure.Features.Shared.Reporting.Contracts.Implementations;
using ST.Infrastructure.Contracts.Interfaces;
using ST.Infrastructure.Logging;
using ST.Infrastructure.Logging.Enrichers;
using ST.Infrastructure.Middleware;
using ST.Infrastructure.Utilities;
using ST.Application.Features.IAM.Users.IntegrationEvents;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ST.Infrastructure.Features.ControlPlane.Tenants.Services;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using Microsoft.Extensions.Http.Resilience;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;
using Quartz.Impl;
using QuestPDF.Infrastructure;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Twilio;
using ST.Infrastructure.Features.IAM.AspNetCoreIdentity.CommandHandlers;
using Microsoft.Azure.StackExchangeRedis;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace ST.Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<AdministratorInvitationSettings>()
            .Bind(configuration.GetSection(AdministratorInvitationSettings.SectionName))
            .Validate(settings => settings.ValidityHours is >= 1 and <= 72, "Invitation validity must be between 1 and 72 hours.")
            .ValidateOnStart();
        services.AddScoped<SignInChallengeFactory>();
        services.AddScoped<SsoCodeStore>();
        services.AddScoped<IExternalIdentityConnection, ExternalIdentityConnection>();
        services.AddScoped<IAdministratorInvitationService, AdministratorInvitationService>();
        services.AddScoped<AdministratorInvitationDelivery>();
        services.AddHostedService<AdministratorInvitationWorker>();
        services.AddScoped<WorkspaceTenantOverride>();
        services.AddScoped<ST.Domain.Shared.Contracts.Common.IWorkspaceTenantOverride>(provider => provider.GetRequiredService<WorkspaceTenantOverride>());
        services.AddScoped<ST.Application.Features.ControlPlane.Demo.IDemoFixture, ST.Infrastructure.Features.ControlPlane.Demo.NavigationDemoFixture>();
        services.AddScoped<ST.Application.Features.ControlPlane.Support.ISupportSessionGuard, ST.Infrastructure.Features.ControlPlane.Support.SupportSessionGuard>();
        services.AddScoped<ST.Application.Features.ControlPlane.Tenants.Contracts.ITenantWorkspaceProvisioner, ST.Infrastructure.Features.ControlPlane.Provisioning.TenantWorkspaceProvisioner>();
        services.AddSingleton(JsonSerializerOptionsFactory.Create());
        services.Configure<ObservabilitySettings>(configuration.GetSection(ObservabilitySettings.SectionName));
        services.Configure<AuthProviderSettings>(configuration.GetSection(AuthProviderSettings.SectionName));
        services.Configure<ApiSettings>(configuration.GetSection(ApiSettings.SectionName));
        services.Configure<MfaSettings>(configuration.GetSection(MfaSettings.SectionName));
        services
            .AddOptions<PasswordRecoverySettings>()
            .Bind(configuration.GetSection(PasswordRecoverySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<FeatureFlagSettings>()
            .Bind(configuration.GetSection(FeatureFlagSettings.SectionName))
            .Validate(IsValidFeatureFlagProvider, "FeatureFlags:Provider must be Configuration.")
            .ValidateOnStart();
        services
            .AddOptions<ReportingSettings>()
            .Bind(configuration.GetSection(ReportingSettings.SectionName))
            .Validate(IsValidReportingSettings, "Reporting:QuestPdf:License must be Community, Professional, or Enterprise.")
            .ValidateOnStart();
        services
            .AddOptions<PaymentSettings>()
            .Bind(configuration.GetSection(PaymentSettings.SectionName))
            .Validate(IsValidPaymentProvider, "Payments:Provider must be NoOp, Stripe, or Mpesa.")
            .ValidateOnStart();
        ConfigurePaymentGateways(services, configuration);
        ConfigureQuestPdf(configuration);
        services
            .AddOptions<ProfileImageStorageSettings>()
            .Bind(configuration.GetSection(ProfileImageStorageSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                IsValidProfileImageStorageProvider,
                "ProfileImageStorage:Provider must be Local, Azurite, AzureBlob, or S3. Blob providers require ContainerUri or ConnectionString plus ContainerName. S3 requires ServiceUrl, BucketName, AccessKey, and SecretKey.")
            .ValidateOnStart();
        services.AddOptions<OrgSettings>()
            .Bind(configuration.GetSection(OrgSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(settings => settings.DefaultTenantId != Guid.Empty, "Tenant:DefaultTenantId must be configured.")
            .ValidateOnStart();
        services.AddOptions<PasskeySettings>()
            .Bind(configuration.GetSection(PasskeySettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var cacheSettings = configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>()
            ?? throw new InvalidOperationException("CacheSettings not found.");

        services.Configure<SessionSettings>(configuration.GetSection(SessionSettings.SectionName));
        ConfigureDistributedCache(services, configuration, cacheSettings);
        ConfigureEmailDelivery(services, configuration);
        ConfigureSms(services, configuration);
        ConfigureSerilogEnrichers(services);
        var observabilitySettings = configuration.GetSection(ObservabilitySettings.SectionName).Get<ObservabilitySettings>() ?? new ObservabilitySettings();
        var authProviderSettings = configuration.GetSection(AuthProviderSettings.SectionName).Get<AuthProviderSettings>() ?? new AuthProviderSettings();
        var messagingSettings = configuration.GetSection(MessagingSettings.SectionName).Get<MessagingSettings>() ?? new MessagingSettings();
        var backgroundJobSettings = configuration.GetSection(BackgroundJobSettings.SectionName).Get<BackgroundJobSettings>() ?? new BackgroundJobSettings();
        ConfigureObservability(services, configuration, environment, observabilitySettings);
        if (backgroundJobSettings.Enabled)
        {
            ConfigureQuartz(services, configuration);
        }

        AddServices(services, authProviderSettings, messagingSettings, backgroundJobSettings);

        return services;
    }

    private static IServiceCollection AddServices(
        this IServiceCollection services,
        AuthProviderSettings authProviderSettings,
        MessagingSettings messagingSettings,
        BackgroundJobSettings backgroundJobSettings)
    {
        if (messagingSettings.Enabled)
        {
            services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        }
        else
        {
            services.AddScoped<IIntegrationEventPublisher, NoOpIntegrationEventPublisher>();
        }

        services.AddHttpClient<IApiService, ApiService>();
        services.AddHttpClient<ST.Application.Features.ControlPlane.Tenants.Contracts.IStampProvisioner, ST.Infrastructure.Features.ControlPlane.Provisioning.GitHubActionsStampProvisioner>();
        services.AddScoped<ICurrentTenantProvider, CurrentTenantProvider>();
        services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
        services.AddScoped<ST.Application.Features.IAM.Menus.IEffectiveNavigation, ST.Application.Features.IAM.Menus.EffectiveNavigation>();
        services.AddScoped<ST.Application.Features.IAM.Users.Contracts.Interfaces.IRoleAvailability, ST.Infrastructure.Features.IAM.AspNetCoreIdentity.QueryHandlers.RoleAvailability>();
        services.AddScoped<ITenantModuleResolver, TenantModuleResolver>();
        services.AddScoped<ST.Application.Features.ControlPlane.Modules.IEffectiveModulePolicy, ST.Infrastructure.Features.ControlPlane.Modules.EffectiveModulePolicy>();
        services.AddScoped<ICurrentActorProvider, CurrentActorProvider>();
        services.AddScoped<IFeatureFlagService, ConfigurationFeatureFlagService>();
        services.AddScoped<IPdfReportService, QuestPdfReportService>();
        services.AddScoped<NoOpPaymentGateway>();
        services.AddScoped<StripePaymentGateway>();
        services.AddScoped<MpesaPaymentGateway>();


        services.AddScoped<IMpesaC2BService>(provider => provider.GetRequiredService<MpesaPaymentGateway>());
        services.AddScoped<IPaymentGateway, RoutedPaymentGateway>();
        services.AddScoped<IPaymentProviderCatalog, PaymentProviderCatalog>();
        services.AddScoped<IPaymentWebhookVerifier, StripeWebhookVerifier>();
        services.AddScoped<LocalProfilePictureStorage>();
        services.AddScoped<AzureBlobProfilePictureStorage>();
        services.AddScoped<S3ProfilePictureStorage>();
        services.AddScoped<IProfilePictureStorage>(sp =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProfileImageStorageSettings>>().Value;
            return GetProfileImageStorageProvider(settings) switch
            {
                ProfileImageStorageProvider.Local => sp.GetRequiredService<LocalProfilePictureStorage>(),
                ProfileImageStorageProvider.Azurite => sp.GetRequiredService<AzureBlobProfilePictureStorage>(),
                ProfileImageStorageProvider.AzureBlob => sp.GetRequiredService<AzureBlobProfilePictureStorage>(),
                ProfileImageStorageProvider.S3 => sp.GetRequiredService<S3ProfilePictureStorage>(),
                _ => throw new InvalidOperationException(
                    $"ProfileImageStorage:Provider '{settings.Provider}' is not supported. " +
                    "Supported values: Local, Azurite, AzureBlob, S3.")
            };
        });

        if (!authProviderSettings.Enabled)
        {
            return services;
        }

        if (GetAuthProvider(authProviderSettings) is not AuthProvider.AspNetCoreIdentity)
        {
            throw new InvalidOperationException(
                $"AuthProvider:Provider '{authProviderSettings.Provider}' is not supported. " +
                "Supported values: AspNetCoreIdentity.");
        }

        services.AddScoped<IEmailService, EmailDeliveryService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<IBackgroundJobService>(_ =>
            backgroundJobSettings.Enabled
                ? new BackgroundJobService(
                    _.GetRequiredService<ISchedulerFactory>(),
                    _.GetRequiredService<ILogger<BackgroundJobService>>())
                : new NoOpBackgroundJobService());
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IOrgSettingsProvider, CachedOrgSettingsProvider>();

        services.AddFido2(options =>
        {
            options.ServerDomain = "localhost";
            options.ServerName = "StarterTemplate";
            options.Origins = new HashSet<string> { "https://localhost:7049", "https://localhost:7129", "http://localhost:5157", "http://localhost:5106" };
        });

        services.AddScoped<IPasskeyService, PasskeyService>();

        return services;
    }

    internal static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services
            .AddOptions<EntraIdSettings>()
            .Bind(configuration.GetSection(EntraIdSettings.SectionName))
            .Validate(IsValidEntraIdSettings, "EntraId requires TenantId, ClientId, and ClientSecret when enabled.")
            .ValidateOnStart();

        var jwtSettings = new JwtSettings();
        configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

        services.AddSingleton(jwtSettings);

        if (jwtSettings == null)
            throw new InvalidOperationException("JwtSettings not found in configuration");

        services.AddSingleton(sp =>
        {
            var jwtSettings = sp.GetRequiredService<JwtSettings>();

            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwtSettings.GetSymmetricSecurityKey(),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkew),
                RequireExpirationTime = true,
                RequireSignedTokens = true,
            };
        });

        services.Configure<IdentityOptions>(ConfigureIdentityOptions);

        var entraIdSettings = configuration.GetSection(EntraIdSettings.SectionName).Get<EntraIdSettings>()
            ?? new EntraIdSettings();

        // Authentication with JWT
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            ConfigureJwtBearer(options, jwtSettings);
        });

        var google = configuration.GetSection(GoogleSsoSettings.SectionName).Get<GoogleSsoSettings>() ?? new();
        var external = configuration.GetSection(ExternalSignInSettings.SectionName).Get<ExternalSignInSettings>() ?? new();
        services.AddOptions<ExternalSignInSettings>().Bind(configuration.GetSection(ExternalSignInSettings.SectionName));
        services.AddOptions<GoogleSsoSettings>().Bind(configuration.GetSection(GoogleSsoSettings.SectionName))
            .Validate(settings => !settings.Enabled || (!string.IsNullOrWhiteSpace(settings.ClientId)
                && !string.IsNullOrWhiteSpace(settings.ClientSecret)), "Google sign-in requires its client ID and secret.")
            .ValidateOnStart();

        if ((entraIdSettings.Enabled || google.Enabled) && (!IsExternalOrigin(external.PublicApiOrigin)
            || external.ClientOrigins.Length == 0 || external.ClientOrigins.Any(origin => !IsExternalOrigin(origin))))
        {
            throw new InvalidOperationException("Enabled external sign-in requires an approved public API origin and frontend origins.");
        }

        if (entraIdSettings.Enabled)
        {
            authenticationBuilder.AddOpenIdConnect("EntraId", options =>
                ConfigureExternalProvider(options, entraIdSettings.Authority, entraIdSettings.ClientId,
                    entraIdSettings.ClientSecret, entraIdSettings.CallbackPath, external.PublicApiOrigin));
        }

        if (google.Enabled)
        {
            authenticationBuilder.AddOpenIdConnect("Google", options =>
                ConfigureExternalProvider(options, "https://accounts.google.com", google.ClientId, google.ClientSecret,
                    google.CallbackPath, external.PublicApiOrigin));
        }

        var passkeySettings = configuration.GetSection(PasskeySettings.SectionName).Get<PasskeySettings>()
            ?? new PasskeySettings();

        if (passkeySettings.Enabled)
        {
            services.AddFido2(options =>
            {
                options.ServerDomain = passkeySettings.ServerDomain;
                options.ServerName = passkeySettings.ServerName;
                options.Origins = new System.Collections.Generic.HashSet<string>(passkeySettings.Origins);
                options.TimestampDriftTolerance = 300000;
            });
        }

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromHours(24); // 24 hours for email tokens
        });
    }

    internal static void ConfigureIdentityOptions(IdentityOptions options)
    {
        options.ClaimsIdentity.UserNameClaimType = "Username";

        // User settings
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        options.User.RequireUniqueEmail = true;

        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.SignIn.RequireConfirmedAccount = false;

        // Token Providers
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
        options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
        options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
    }

    internal static void ConfigureJwtBearer(JwtBearerOptions options, JwtSettings jwtSettings)
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = jwtSettings?.Issuer,
            ValidIssuers = jwtSettings?.ValidIssuers,
            ValidAudience = jwtSettings?.Audience,
            ValidAudiences = jwtSettings?.ValidAudiences,
            IssuerSigningKey = jwtSettings?.GetSymmetricSecurityKey(),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,

            // CUSTOM LIFETIME VALIDATOR to handle NotBefore issues
            LifetimeValidator = (notBefore, expires, token, parameters) =>
            {
                var now = DateTime.UtcNow;

                // Check expiration (required)
                if (expires.HasValue && expires.Value < now)
                {
                    return false; // Token expired
                }

                // Check not before (lenient - allow if missing or if time is close)
                if (notBefore.HasValue && notBefore.Value > now.AddMinutes(1))
                {
                    return false; // Token not yet valid (with 1 min tolerance)
                }

                return true; // Token is valid
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.ContentType = "application/json";

                    var expirationTime = context.Exception is SecurityTokenExpiredException expiredException
                        ? expiredException.Expires.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                        : string.Empty;

                    var errorMessage = new
                    {
                        context.Response.StatusCode,
                        Message = "Token has expired.",
                        ExpirationTime = expirationTime,
                        CurrentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                        Error = context.Exception.Message
                    };

                    return context.Response.WriteAsync(JsonSerializer.Serialize(errorMessage));
                }
                else
                {
                    var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("JwtAuthentication");

                    // Check for NotBefore issues specifically
                    if (context.Exception.Message.Contains("NotBefore", StringComparison.OrdinalIgnoreCase) ||
                    context.Exception.Message.Contains("not yet valid", StringComparison.OrdinalIgnoreCase))
                    {
                        ServiceLogDefinitions.LogJwtAuthenticationFailed(logger, "Token is not yet valid", context.Exception);
                    }
                    else
                    {
                        ServiceLogDefinitions.LogJwtAuthenticationFailed(logger, "Token validation failed", context.Exception);
                    }
                }

                return Task.CompletedTask;
            }
        };
    }

    private static void ConfigureDistributedCache(IServiceCollection services, IConfiguration configuration, CacheSettings cacheSettings)
    {
        var provider = GetCacheProvider(cacheSettings);

        switch (provider)
        {
            case CacheProvider.Memory:
                services.AddDistributedMemoryCache();
                break;

            case CacheProvider.Redis:
                ValidateConnectionString(cacheSettings.Redis?.ConnectionString, "Redis");
                RegisterRedisCache(services, cacheSettings.Redis?.ConnectionString!);
                break;

            case CacheProvider.AzureManagedRedis:
                ValidateConnectionString(cacheSettings.Azure?.ConnectionString, "AzureManagedRedis");
                if (cacheSettings.Azure?.UseEntraId == true)
                {
                    RegisterAzureManagedRedisWithEntraId(services, cacheSettings.Azure);
                }
                else
                {
                    RegisterRedisCache(services, cacheSettings.Azure!.ConnectionString!);
                }
                break;

            case CacheProvider.Invalid:
            default:
                throw new InvalidOperationException(
                    $"CacheSettings:Provider '{cacheSettings.Provider}' is not supported. " +
                    "Supported values: Auto, Memory, Redis, AzureManagedRedis.");
        }

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(1)
            };
            options.MaximumPayloadBytes = 1024 * 1024; // 1MB limit for L2
        });

        services.AddSingleton<ICacheService, HybridCacheService>();

        services.AddOutputCache(options =>
        {
            options.AddPolicy("LookupCachePolicy", builder =>
                builder.Expire(TimeSpan.FromMinutes(5)).Tag("lookups"));
        });
    }

    private static bool IsExternalOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback))
        && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.UserInfo)
        && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    private static void ConfigureExternalProvider(OpenIdConnectOptions options, string authority, string clientId,
        string clientSecret, string callbackPath, string publicApiOrigin)
    {
        options.Authority = authority;
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
        options.CallbackPath = callbackPath;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.MapInboundClaims = false;
        options.SaveTokens = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.RedirectUri = new Uri(new Uri(publicApiOrigin), callbackPath).AbsoluteUri;
            return Task.CompletedTask;
        };
        options.Events.OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is not ClaimsIdentity identity
                || string.IsNullOrWhiteSpace(context.SecurityToken.Issuer)
                || string.IsNullOrWhiteSpace(identity.FindFirst("sub")?.Value))
            {
                context.Fail("Required provider identity claims are missing.");
                return Task.CompletedTask;
            }

            foreach (var existing in identity.FindAll("st_validated_issuer").ToArray())
            {
                identity.RemoveClaim(existing);
            }

            identity.AddClaim(new Claim("st_validated_issuer", context.SecurityToken.Issuer));
            return Task.CompletedTask;
        };
    }

    private static void RegisterRedisCache(IServiceCollection services, string connectionString)
    {
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
        {
            var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(connectionString);
            configOptions.AbortOnConnectFail = false;
            return StackExchange.Redis.ConnectionMultiplexer.Connect(configOptions);
        });

        RegisterStackExchangeRedisCache(services);
    }

    private static void RegisterAzureManagedRedisWithEntraId(
        IServiceCollection services,
        AzureCacheSettings settings)
    {
        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
        {
            var configOptions = StackExchange.Redis.ConfigurationOptions.Parse(settings.ConnectionString!);
            configOptions.Protocol = StackExchange.Redis.RedisProtocol.Resp3;
            configOptions.Ssl = true;
            configOptions.AbortOnConnectFail = false;
            configOptions.Password = null;

            var credentialOptions = new Azure.Identity.DefaultAzureCredentialOptions();
            if (!string.IsNullOrWhiteSpace(settings.PrincipalId))
            {
                credentialOptions.ManagedIdentityClientId = settings.PrincipalId;
            }

            configOptions.ConfigureForAzureWithTokenCredentialAsync(
                new Azure.Identity.DefaultAzureCredential(credentialOptions)).GetAwaiter().GetResult();

            return StackExchange.Redis.ConnectionMultiplexer.Connect(configOptions);
        });

        RegisterStackExchangeRedisCache(services);
    }

    private static void RegisterStackExchangeRedisCache(IServiceCollection services)
    {
        services.AddStackExchangeRedisCache(options => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<StackExchange.Redis.IConnectionMultiplexer>((options, multiplexer) =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer);
            });
    }

    private static bool IsConfiguredConnectionString(string? connectionString)
    {
        return !string.IsNullOrWhiteSpace(connectionString) &&
            !connectionString.Contains("your-", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("replace", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("set_via", StringComparison.OrdinalIgnoreCase);
    }

    private static CacheProvider GetCacheProvider(CacheSettings settings)
    {
        var configuredProvider = settings.Provider?.Trim() ?? "Auto";
        if (configuredProvider.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            if (IsConfiguredConnectionString(settings.Azure?.ConnectionString))
            {
                return CacheProvider.AzureManagedRedis;
            }

            return IsConfiguredConnectionString(settings.Redis.ConnectionString)
                ? CacheProvider.Redis
                : CacheProvider.Memory;
        }

        return configuredProvider switch
        {
            var provider when provider.Equals("Memory", StringComparison.OrdinalIgnoreCase) => CacheProvider.Memory,
            var provider when provider.Equals("Redis", StringComparison.OrdinalIgnoreCase) &&
                              IsConfiguredConnectionString(settings.Redis.ConnectionString) => CacheProvider.Redis,
            var provider when provider.Equals("AzureManagedRedis", StringComparison.OrdinalIgnoreCase) &&
                              IsConfiguredConnectionString(settings.Azure?.ConnectionString) => CacheProvider.AzureManagedRedis,
            _ => CacheProvider.Invalid
        };
    }

    private static void ValidateConnectionString(string? connectionString, string providerName)
    {
        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Equals("SET_VIA_USER_SECRETS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Configuration Error: The connection string for provider '{providerName}' is missing or has a placeholder value.");
        }
    }

    private static bool IsValidProfileImageStorageProvider(ProfileImageStorageSettings settings)
    {
        return GetProfileImageStorageProvider(settings) switch
        {
            ProfileImageStorageProvider.Local => true,
            ProfileImageStorageProvider.Azurite => IsBlobProfileImageStorageConfigured(settings.Azurite),
            ProfileImageStorageProvider.AzureBlob => IsAzureBlobProfileImageStorageConfigured(settings),
            ProfileImageStorageProvider.S3 => IsS3ProfileImageStorageConfigured(settings.S3),
            _ => false
        };
    }

    private static bool IsAzureBlobProfileImageStorageConfigured(ProfileImageStorageSettings settings)
    {
        return IsBlobProfileImageStorageConfigured(settings.AzureBlob);
    }

    private static bool IsBlobProfileImageStorageConfigured(AzureBlobProfileImageStorageSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.ContainerUri) ||
            (!string.IsNullOrWhiteSpace(settings.ConnectionString) &&
             !string.IsNullOrWhiteSpace(settings.ContainerName));
    }

    private static bool IsS3ProfileImageStorageConfigured(S3ProfileImageStorageSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.ServiceUrl) &&
               !string.IsNullOrWhiteSpace(settings.BucketName) &&
               !string.IsNullOrWhiteSpace(settings.AccessKey) &&
               !string.IsNullOrWhiteSpace(settings.SecretKey);
    }

    private static ProfileImageStorageProvider GetProfileImageStorageProvider(ProfileImageStorageSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            return ProfileImageStorageProvider.Local;
        }

        return settings.Provider.Trim() switch
        {
            var provider when provider.Equals("Local", StringComparison.OrdinalIgnoreCase) => ProfileImageStorageProvider.Local,
            var provider when provider.Equals("Azurite", StringComparison.OrdinalIgnoreCase) => ProfileImageStorageProvider.Azurite,
            var provider when provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase) => ProfileImageStorageProvider.AzureBlob,
            var provider when provider.Equals("S3", StringComparison.OrdinalIgnoreCase) => ProfileImageStorageProvider.S3,
            _ => ProfileImageStorageProvider.Invalid
        };
    }

    private static AuthProvider GetAuthProvider(AuthProviderSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            return AuthProvider.AspNetCoreIdentity;
        }

        return settings.Provider.Trim() switch
        {
            var provider when provider.Equals("AspNetCoreIdentity", StringComparison.OrdinalIgnoreCase) => AuthProvider.AspNetCoreIdentity,
            _ => AuthProvider.Invalid
        };
    }

    private static void ConfigureSerilogEnrichers(IServiceCollection services)
    {
        services.AddSingleton<ILogEventEnricher, CorrelationIdEnricher>();
        services.AddSingleton<ILogEventEnricher, IPAddressEnricher>();
    }

    /// <summary>
    /// Registers OpenTelemetry tracing, metrics, and Azure Monitor export.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Architecture decision:</b> We use the Azure Monitor OpenTelemetry Distro
    /// (<c>Azure.Monitor.OpenTelemetry.AspNetCore</c>). The Distro is
    /// Microsoft's strategic direction â€” it follows the OTel standard, meaning
    /// you can swap the exporter (Azure Monitor â†’ Jaeger/Grafana/etc.) by changing
    /// one line.
    /// </para>
    /// <para>
    /// <b>What the Distro includes automatically</b> (no extra packages needed):
    /// ASP.NET Core request tracing, HttpClient tracing, SQLClient tracing,
    /// Azure resource detection (App Service, VM, Container Apps), live metrics.
    /// </para>
    /// <para>
    /// <b>What we add on top:</b>
    /// Runtime metrics (GC, ThreadPool, memory) via
    /// <c>OpenTelemetry.Instrumentation.Runtime</c>.
    /// </para>
    /// <para>
    /// <b>Serilog relationship:</b> Serilog continues to handle log routing
    /// (Console, Seq, File). The OTel pipeline handles traces and metrics.
    /// Logs flow to Azure Monitor via Serilog's ILogger integration with
    /// <c>ReadFrom.Services()</c> â€” not via a Serilog sink.
    /// </para>
    /// </remarks>
    public static IServiceCollection ConfigureObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ObservabilitySettings observabilitySettings)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(observabilitySettings, nameof(observabilitySettings));
        if (!observabilitySettings.Enabled)
        {
            return services;
        }

        var connectionString = observabilitySettings.AzureMonitor.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString =
                configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ??
                configuration["ApplicationInsights:ConnectionString"];
        }
        var hasAzureMonitor = !string.IsNullOrWhiteSpace(connectionString);
        var hasOtlp = !string.IsNullOrWhiteSpace(observabilitySettings.Otlp?.Endpoint);

        if (!hasAzureMonitor && !hasOtlp)
        {
            Console.WriteLine(
                "[WARN] Observability is enabled but no exporter (AzureMonitor or Otlp) is configured. " +
                "OpenTelemetry export is disabled.");
            return services;
        }

        var otelBuilder = services.AddOpenTelemetry();

        otelBuilder.ConfigureResource(resource => resource
            .AddService(serviceName: observabilitySettings.ServiceName, serviceVersion: "1.0.0")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment.EnvironmentName,
                ["service.namespace"] = observabilitySettings.ServiceNamespace
            }));


        otelBuilder.WithTracing(tracing =>
        {
            tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health") &&
                        !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddSource("ST.Cache");

            if (hasOtlp)
            {
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(observabilitySettings.Otlp!.Endpoint);
                    if (!string.IsNullOrWhiteSpace(observabilitySettings.Otlp.Headers))
                    {
                        options.Headers = observabilitySettings.Otlp.Headers;
                    }
                });
            }
        });

        otelBuilder.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("ST.Cache")
                .AddMeter("ST.TenantMetrics");

            if (hasOtlp)
            {
                metrics.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(observabilitySettings.Otlp!.Endpoint);
                    if (!string.IsNullOrWhiteSpace(observabilitySettings.Otlp.Headers))
                    {
                        options.Headers = observabilitySettings.Otlp.Headers;
                    }
                });
            }
        });

        if (hasAzureMonitor)
        {
            otelBuilder.UseAzureMonitor(options =>
            {
                options.ConnectionString = connectionString;
            });
        }

        return services;
    }

    private static void ConfigureSms(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsSettings>(configuration.GetSection(SmsSettings.SectionName));

        var smsSettings = configuration.GetSection(SmsSettings.SectionName).Get<SmsSettings>();
        var twilio = smsSettings?.Twilio;

        if (twilio is not null &&
            !string.IsNullOrWhiteSpace(twilio.AccountSid) &&
            !string.IsNullOrWhiteSpace(twilio.AuthToken))
        {
            TwilioClient.Init(twilio.AccountSid, twilio.AuthToken);
        }


    }

    private static void ConfigureEmailDelivery(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<EmailSettings>()
            .Bind(configuration.GetSection(EmailSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(IsValidEmailProvider, "EmailSettings:Provider must be NoOp, LocalMailpit, SendGrid, AzureCommunication, or Resend.")
            .ValidateOnStart();

        var settings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        services.AddHttpClient("Email.SendGrid", client =>
        {
            if (!string.IsNullOrWhiteSpace(settings.SendGrid.Endpoint) &&
                Uri.TryCreate(settings.SendGrid.Endpoint, UriKind.Absolute, out var endpoint))
            {
                client.BaseAddress = endpoint;
            }
        });

        services.AddHttpClient("Email.Resend", client =>
        {
            if (!string.IsNullOrWhiteSpace(settings.Resend.Endpoint) &&
                Uri.TryCreate(settings.Resend.Endpoint, UriKind.Absolute, out var endpoint))
            {
                client.BaseAddress = endpoint;
            }
        });

        if (!string.IsNullOrWhiteSpace(settings.AzureCommunication.ConnectionString) &&
            settings.AzureCommunication.ConnectionString != "SET_IN_USER_SECRETS")
        {
            try
            {
                services.AddSingleton(new Azure.Communication.Email.EmailClient(settings.AzureCommunication.ConnectionString));
            }
            catch
            {
                // Ignore invalid connection string at startup. If the provider is set to AzureCommunication, ValidateOnStart will catch it.
            }
        }
    }

    private static bool IsValidEmailProvider(EmailSettings settings)
    {
        return settings is not null && (string.IsNullOrWhiteSpace(settings.FromAddress)
            ? false
            : GetEmailProvider(settings) switch
            {
                EmailProvider.NoOp => true,
                EmailProvider.LocalMailpit => !string.IsNullOrWhiteSpace(settings.LocalMailpit.Host) &&
                    settings.LocalMailpit.Port > 0,
                EmailProvider.SendGrid => !string.IsNullOrWhiteSpace(settings.SendGrid.Endpoint) &&
                    Uri.TryCreate(settings.SendGrid.Endpoint, UriKind.Absolute, out _),
                EmailProvider.AzureCommunication => !string.IsNullOrWhiteSpace(settings.AzureCommunication.ConnectionString),
                EmailProvider.Resend => !string.IsNullOrWhiteSpace(settings.Resend.Endpoint) &&
                    Uri.TryCreate(settings.Resend.Endpoint, UriKind.Absolute, out _),
                _ => false
            });
    }

    private static bool IsValidFeatureFlagProvider(FeatureFlagSettings settings)
    {
        return settings is not null &&
            GetFeatureFlagProvider(settings) is FeatureFlagProvider.Configuration;
    }

    private static bool IsValidReportingSettings(ReportingSettings settings)
    {
        return settings is not null &&
            GetQuestPdfLicense(settings.QuestPdf) is not QuestPdfLicense.Invalid;
    }

    private static bool IsValidPaymentProvider(PaymentSettings settings) =>
        settings is not null &&
        PaymentProviderParser.Parse(settings.Provider) is not PaymentProviderKind.Invalid;

    private static bool IsValidEntraIdSettings(EntraIdSettings settings)
    {
        if (settings is null)
        {
            return false;
        }

        return !settings.Enabled ||
            (!string.IsNullOrWhiteSpace(settings.TenantId) &&
             !string.IsNullOrWhiteSpace(settings.ClientId) &&
             !string.IsNullOrWhiteSpace(settings.ClientSecret));
    }

    private static void ConfigurePaymentGateways(IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(PaymentSettings.SectionName).Get<PaymentSettings>() ?? new PaymentSettings();

        services.AddHttpClient("Payments.Stripe", client =>
        {
            if (!string.IsNullOrWhiteSpace(settings.Stripe.CheckoutSessionsEndpoint) &&
                Uri.TryCreate(settings.Stripe.CheckoutSessionsEndpoint, UriKind.Absolute, out var endpoint))
            {
                client.BaseAddress = endpoint;
            }
        });

        services.AddHttpClient("Payments.Mpesa.Auth", client =>
        {
            if (!string.IsNullOrWhiteSpace(settings.Mpesa.BaseUrl) &&
                Uri.TryCreate(settings.Mpesa.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "StarterTemplate-HttpClient/1.0");
        });

        services.AddHttpClient("Payments.Mpesa", client =>
        {
            if (!string.IsNullOrWhiteSpace(settings.Mpesa.BaseUrl) &&
                Uri.TryCreate(settings.Mpesa.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                client.BaseAddress = baseUri;
            }
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "StarterTemplate-HttpClient/1.0");
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 1;
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        });
    }

    private static void ConfigureQuestPdf(IConfiguration configuration)
    {
        var settings = configuration.GetSection(ReportingSettings.SectionName).Get<ReportingSettings>()
            ?? new ReportingSettings();

        if (!settings.QuestPdf.Enabled)
        {
            return;
        }

        QuestPDF.Settings.License = GetQuestPdfLicense(settings.QuestPdf) switch
        {
            QuestPdfLicense.Community => LicenseType.Community,
            QuestPdfLicense.Professional => LicenseType.Professional,
            QuestPdfLicense.Enterprise => LicenseType.Enterprise,
            _ => throw new InvalidOperationException(
                $"Reporting:QuestPdf:License '{settings.QuestPdf.License}' is not supported. " +
                "Supported values: Community, Professional, Enterprise.")
        };
    }

    private static void ConfigureQuartz(IServiceCollection services, IConfiguration configuration)
    {
        var dbProvider = configuration.GetValue<string>($"{DatabaseSettings.SectionName}:Provider") ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string not found in configuration");

        services.AddQuartz(q =>
        {
            // Use a unique ID for this scheduler instance
            q.ConfigureScheduler(options => options.InstanceId = "BT_Q_Scheduler");

            q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();

            q.UsePersistentStore(s =>
            {
                s.ConfigureStore(options => options.StoreJobDataAsStrings = true);

                s.UseNewtonsoftJsonSerializer();

                if (dbProvider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
                {
                    s.UsePostgres(sq =>
                    {
                        sq.ConnectionString = connectionString;
                    });
                }
                else
                {
                    s.UseSqlServer(sq =>
                    {
                        sq.ConnectionString = connectionString;
                    });
                }
            });
        });

        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });

    }

    public static IApplicationBuilder UseInfrastructureLoggingMiddleware(this IApplicationBuilder app)
    {
        // Serilog request logging must be first â€” it wraps the entire pipeline
        // so it can measure the full request duration including auth, routing etc.
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            // Log level based on outcome â€” errors and 5xx are Error, 4xx are Warning
            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= 400
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;

            // Enrich each request log with contextual properties
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);

            };
        });

        // Custom middleware â€” adds X-Correlation-ID response header,
        // optionally logs request bodies at Debug level
        app.UseMiddleware<LoggingMiddleware>();

        return app;
    }

    private enum AuthProvider
    {
        AspNetCoreIdentity,
        Invalid
    }

    private enum ProfileImageStorageProvider
    {
        Local,
        Azurite,
        AzureBlob,
        S3,
        Invalid
    }

    private enum CacheProvider
    {
        Memory,
        Redis,
        AzureManagedRedis,
        Invalid
    }

    private static EmailProvider GetEmailProvider(EmailSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            return EmailProvider.NoOp;
        }

        return settings.Provider.Trim() switch
        {
            var provider when provider.Equals("NoOp", StringComparison.OrdinalIgnoreCase) => EmailProvider.NoOp,
            var provider when provider.Equals("LocalMailpit", StringComparison.OrdinalIgnoreCase) => EmailProvider.LocalMailpit,
            var provider when provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase) => EmailProvider.SendGrid,
            var provider when provider.Equals("AzureCommunication", StringComparison.OrdinalIgnoreCase) => EmailProvider.AzureCommunication,
            var provider when provider.Equals("Resend", StringComparison.OrdinalIgnoreCase) => EmailProvider.Resend,
            _ => EmailProvider.Invalid
        };
    }

    private enum EmailProvider
    {
        NoOp,
        LocalMailpit,
        SendGrid,
        AzureCommunication,
        Resend,
        Invalid
    }

    private static FeatureFlagProvider GetFeatureFlagProvider(FeatureFlagSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            return FeatureFlagProvider.Configuration;
        }

        return settings.Provider.Trim() switch
        {
            var provider when provider.Equals("Configuration", StringComparison.OrdinalIgnoreCase) => FeatureFlagProvider.Configuration,
            _ => FeatureFlagProvider.Invalid
        };
    }

    private enum FeatureFlagProvider
    {
        Configuration,
        Invalid
    }

    private static QuestPdfLicense GetQuestPdfLicense(QuestPdfSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.License))
        {
            return QuestPdfLicense.Community;
        }

        return settings.License.Trim() switch
        {
            var license when license.Equals("Community", StringComparison.OrdinalIgnoreCase) => QuestPdfLicense.Community,
            var license when license.Equals("Professional", StringComparison.OrdinalIgnoreCase) => QuestPdfLicense.Professional,
            var license when license.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) => QuestPdfLicense.Enterprise,
            _ => QuestPdfLicense.Invalid
        };
    }

    private enum QuestPdfLicense
    {
        Community,
        Professional,
        Enterprise,
        Invalid
    }

}

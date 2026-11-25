using Microsoft.Extensions.Hosting;
using ST.Infrastructure.Features.ControlPlane.Kyc;
using System.Security.Cryptography;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ST.Infrastructure.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.IAM.DataContext;
using ST.Persistence.Features.Shared.DataContext;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
// <db:SqlServer>
using Testcontainers.MsSql;
// </db:SqlServer>
// <db:PostgreSql>
using Testcontainers.PostgreSql;
// </db:PostgreSql>
using Xunit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
// <db:SqlServer>
using Microsoft.Data.SqlClient;
// </db:SqlServer>
// <db:PostgreSql>
using Npgsql;
// </db:PostgreSql>
using ST.Domain.Shared;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Tests.Integration.NeutralSlice;

namespace ST.Tests.Integration.TestFixtures;

/// <summary>
/// Integration test host that spins up a fresh database Testcontainer for the build's primary
/// provider (DatabaseProviders.Supported[0]), rewires
/// ALL bounded-context DbContexts to point at it, applies migrations, then runs tests.
///
/// Lifecycle (xUnit IClassFixture + IAsyncLifetime):
///   1. Constructor    — builds the Testcontainer object (no Docker interaction yet).
///   2. InitializeAsync — starts the container, sets connection string, then forces
///                        the host to build by accessing Services, and applies migrations.
///   3. Test methods   — call CreateClient(); host is already built at this point.
///   4. DisposeAsync   — stops and removes the container.
/// </summary>
public class StarterTemplateWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string DatabaseProvider = DatabaseProviders.Supported[0];
    private readonly IDatabaseContainer _dbContainer;
    private readonly IContainer _mailpit;
    private readonly IContainer _kycStorage;
    private readonly string _kycKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    internal Uri MailpitApi => new($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(8025)}");
    private string _containerConnectionString = string.Empty;

    public StarterTemplateWebApplicationFactory()
    {
        // ── Force the environment to Development BEFORE WebApplication.CreateBuilder runs ──
        // Program.cs uses WebApplication.CreateBuilder(args), which reads environment variables
        // *before* ConfigureWebHost gets called. If the CI runs with ASPNETCORE_ENVIRONMENT=Production,
        // Program.cs will attempt to wire up Azure Key Vault and crash with an empty URI.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

        // Force test configuration early so builder.Configuration reads it in Program.cs
        Environment.SetEnvironmentVariable("CacheSettings__Provider", "Memory");
        Environment.SetEnvironmentVariable("DevelopmentSeed__Enabled", "false");
        Environment.SetEnvironmentVariable("JwtSettings__SecretKey", "StarterCertificationOnlySigningKey012345678901234567890123456789");

        _kycStorage = new ContainerBuilder().WithImage("mcr.microsoft.com/azure-storage/azurite:3.35.0")
            .WithEnvironment("AZURITE_ACCOUNTS", $"sttest:{_kycKey}").WithPortBinding(10000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(10000)).Build();

        _mailpit = new ContainerBuilder().WithImage("axllent/mailpit:v1.27")
            .WithPortBinding(1025, true).WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1025)).Build();

        IDatabaseContainer? container = null;
        // <db:SqlServer>
        if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.SqlServer))
        {
            container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Strong@Passw0rd!")
                .Build();
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.PostgreSql))
        {
            container = new PostgreSqlBuilder()
                .WithImage("postgres:15-alpine")
                .WithDatabase("starter_api_tests")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
        }
        // </db:PostgreSql>
        _dbContainer = container ?? throw new NotSupportedException(DatabaseProvider);
    }

    internal async Task RegisterTenantAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();

        if (await db.Tenants.AnyAsync(x => x.Id == id))
        {
            return;
        }

        var tenant = new Tenant { Id = id, Identifier = $"test-{id:N}", DisplayName = "Test workspace", HostName = $"{id:N}.invalid", DeploymentStampId = Guid.Parse("0194f700-0000-7000-8000-000000000001"), CreatedBy = "test-fixture", MaxUsers = 100 };
        tenant.Approve("test-fixture", DateTimeOffset.UnixEpoch, "review-1");
        tenant.MarkAsActive();

        foreach (var module in new[] { "Core", "IAM" })
        {
            tenant.Modules.Add(new TenantModule { TenantId = id, ModuleKey = module, CreatedBy = "test-fixture" });
        }

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // ── Suppress the development identity seeder and remove external deps ────
        // Program.cs calls SeedDevelopmentIdentityAsync() during host startup (before
        // our schema-creation step can run). If the seeder fires against an empty DB it
        // throws, the host fails to start, and CreateClient() gets
        // "server has not been started". Disabling it here is safe — integration tests
        // set up their own seed data inside each test method.
        //
        // NOTE: We do NOT call UseEnvironment("Testing") here because the application's
        // Program.cs has a !IsDevelopment() guard that enables Azure Key Vault, which
        // would immediately throw "KeyVault:Uri is not configured." We MUST explicitly
        // set the environment to "Development" here to guarantee that guard is bypassed,
        // even if the CI pipeline sets ASPNETCORE_ENVIRONMENT=Production.
        builder.UseEnvironment("Development");

        // Derived storage tests opt into their own disposable Azurite; never inherit workstation KYC credentials.
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["KycDocuments:Provider"] = "Azurite",
            ["KycDocuments:ConnectionString"] = $"DefaultEndpointsProtocol=http;AccountName=sttest;AccountKey={_kycKey};BlobEndpoint=http://{_kycStorage.Hostname}:{_kycStorage.GetMappedPublicPort(10000)}/sttest;",
            ["KycDocuments:ContainerName"] = "test-kyc-evidence",
            ["KycDocuments:ErasureLedgerContainerName"] = "test-kyc-ledger",
            ["KycDocuments:ContainerUri"] = ""
        }));

        builder.ConfigureTestServices(services =>
        {
            // Storage tests explicitly drain the durable processor with their own isolated scanner/storage.
            // A second factory sharing the SQL fixture must not race using a different blob account.
            foreach (var hosted in services.Where(item => item.ServiceType == typeof(IHostedService)
                && item.ImplementationType == typeof(KycDocumentSafetyWorker)).ToArray())
            {
                services.Remove(hosted);
            }

            // ── 1. Auth: replace real JWT/Cookie stack with test-header-based handler ──
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Certification";
                options.DefaultChallengeScheme = "Certification";
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.AuthenticationScheme,
                options => { })
            .AddPolicyScheme("Certification", null, options => options.ForwardDefaultSelector = context =>
                context.Request.Headers.Authorization.ToString().StartsWith(TestAuthHandler.AuthenticationScheme, StringComparison.Ordinal)
                    ? TestAuthHandler.AuthenticationScheme : JwtBearerDefaults.AuthenticationScheme);

            services.Configure<EmailSettings>(options =>
            {
                options.Provider = "LocalMailpit";
                options.ClientBaseUrl = "https://starter.certification.test/";
                options.FromAddress = "starter@example.test";
                options.LocalMailpit.Host = _mailpit.Hostname;
                options.LocalMailpit.Port = _mailpit.GetMappedPublicPort(1025);
            });


            // ── 2. Rewire ALL bounded-context DbContexts to the Testcontainer ──────────
            //
            // Standard WebApplicationFactory pattern:
            //   a) Remove the existing DbContextOptions<T> descriptors registered by Program.cs
            //   b) Re-register them directly with our container connection string
            //
            // <db:SqlServer>
            if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.SqlServer))
            {
                RewireDbContext<ControlPlaneDBContext, ControlPlaneSqlServerDBContext>(services, _containerConnectionString);
                RewireDbContext<IamDBContext, IamSqlServerDBContext>(services, _containerConnectionString);
                RewireDbContext<SharedDBContext, SharedSqlServerDBContext>(services, _containerConnectionString);
            }
            // </db:SqlServer>
            // <db:PostgreSql>
            if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.PostgreSql))
            {
                RewireDbContext<ControlPlaneDBContext, ControlPlanePostgreSqlDBContext>(services, _containerConnectionString);
                RewireDbContext<IamDBContext, IamPostgreSqlDBContext>(services, _containerConnectionString);
                RewireDbContext<SharedDBContext, SharedPostgreSqlDBContext>(services, _containerConnectionString);
            }
            // </db:PostgreSql>
            services.AddControllers().AddApplicationPart(typeof(NeutralSliceController).Assembly);
            services.AddDbContext<NeutralSliceDBContext>(options => UseContainerDatabase(options, _containerConnectionString));
            services.AddScoped<NeutralSliceUnitOfWork>();
            services.AddTransient<IRequestHandler<CreateNeutralNote, Guid>, CreateNeutralNoteHandler>();
            services.AddTransient<IRequestHandler<ReadNeutralNote, string?>, ReadNeutralNoteHandler>();

        });
    }

    private static void RewireDbContext<TContext, TProviderContext>(
        IServiceCollection services,
        string connectionString)
        where TContext : DbContext
        where TProviderContext : TContext
    {
        // Remove existing DbContextOptions descriptors registered by Program.cs
        var descriptorsToRemove = services
            .Where(d =>
                d.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>)
             || d.ServiceType == typeof(IDbContextOptionsConfiguration<TProviderContext>)
             || d.ServiceType == typeof(DbContextOptions<TContext>)
             || d.ServiceType == typeof(DbContextOptions<TProviderContext>))
            .ToList();

        foreach (var d in descriptorsToRemove)
        {
            services.Remove(d);
        }

        // Remove existing concrete context registrations
        var concreteToRemove = services
            .Where(d =>
                (d.ServiceType == typeof(TContext) && d.ImplementationType == typeof(TProviderContext))
             || d.ServiceType == typeof(TProviderContext))
            .ToList();

        foreach (var d in concreteToRemove)
        {
            services.Remove(d);
        }

        // Re-register pointing at the Testcontainer. No migration settings needed
        // because we use EnsureCreated / CreateTablesAsync for test schema setup.
        services.AddDbContext<TContext, TProviderContext>((_, options) =>
            UseContainerDatabase(options, connectionString));
    }

    private static void UseContainerDatabase(DbContextOptionsBuilder options, string connectionString)
    {
        // <db:SqlServer>
        if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.SqlServer))
        {
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(60));
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.PostgreSql))
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(60));
        }
        // </db:PostgreSql>
    }

    public async Task InitializeAsync()
    {
        // Step 1: Start the container — must happen before anything else.
        await _dbContainer.StartAsync();
        await _mailpit.StartAsync();
        await _kycStorage.StartAsync();
        _containerConnectionString = _dbContainer.GetConnectionString();
        // <db:SqlServer>
        if (DatabaseProviders.Is(DatabaseProvider, DatabaseProviders.SqlServer))
        {
            _containerConnectionString = _containerConnectionString.TrimEnd(';') + ";TrustServerCertificate=True;Encrypt=False;";
        }
        // </db:SqlServer>

        // Step 2: Force the WebApplicationFactory to build the host now (by accessing Services).
        // _containerConnectionString is populated above so ConfigureTestServices wires correctly.
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        // Step 3: Create schema for all bounded contexts.
        //
        // Strategy: EnsureCreatedAsync() for the FIRST context creates the database + its tables.
        // For subsequent contexts we call IRelationalDatabaseCreator.CreateTablesAsync() directly,
        // because EnsureCreated() is NOT additive — once the database has ANY tables, it returns
        // false and skips CreateTables() for every subsequent call.
        //
        // We catch the provider's "object already exists" errors to handle tables/indexes
        // that appear in multiple context models.
        //
        // Order matters: ControlPlane first — TenantConnectionInterceptor queries [Tenants] on
        // every connection open before any other table can be touched.
        await sp.GetRequiredService<ControlPlaneDBContext>().Database.EnsureCreatedAsync();
        await CreateTablesAsync(sp.GetRequiredService<IamDBContext>());
        await CreateTablesAsync(sp.GetRequiredService<SharedDBContext>());
        await CreateTablesAsync(sp.GetRequiredService<NeutralSliceDBContext>());
    }

    new public async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _kycStorage.DisposeAsync();
        await _mailpit.DisposeAsync();
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates the tables defined in <paramref name="context"/>'s model against the
    /// already-existing test database, silently ignoring "already exists" errors for tables
    /// or indexes created by a previously-called context.
    /// </summary>
    private static async Task CreateTablesAsync(DbContext context)
    {
        try
        {
            var creator = context.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
        // <db:SqlServer>
        catch (SqlException ex)
            when (ex.Number is 2714 or 1913)
        {
            // 2714 = There is already an object named '…' in the database.
            // 1913 = The operation failed because an index or statistics with name '…' already exists.
            // Both are expected when contexts share a database and tables/indexes overlap.
        }
        // </db:SqlServer>
        // <db:PostgreSql>
        catch (PostgresException ex)
            when (ex.SqlState is PostgresErrorCodes.DuplicateTable or PostgresErrorCodes.DuplicateObject)
        {
            // 42P07 duplicate_table / 42710 duplicate_object: expected when contexts share tables or indexes.
        }
        // </db:PostgreSql>
    }
}

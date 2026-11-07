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
using Testcontainers.MsSql;
using Xunit;

namespace ST.Tests.Integration.TestFixtures;

/// <summary>
/// Integration test host that spins up a fresh SQL Server Testcontainer, rewires
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
    private readonly MsSqlContainer _dbContainer;
    private readonly IContainer _mailpit;
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

        _mailpit = new ContainerBuilder().WithImage("axllent/mailpit:v1.27")
            .WithPortBinding(1025, true).WithPortBinding(8025, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1025)).Build();

        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong@Passw0rd!")
            .Build();
    }

    internal async Task RegisterTenantAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
        if (await db.Tenants.AnyAsync(x=>x.Id==id)) return;
        var tenant = new ST.Domain.Features.ControlPlane.Tenants.Entities.Tenant { Id=id,Identifier=$"test-{id:N}",DisplayName="Test workspace",HostName=$"{id:N}.invalid",DeploymentStampId=Guid.Parse("0194f700-0000-7000-8000-000000000001"),CreatedBy="test-fixture",MaxUsers=100 };
        tenant.Approve("test-fixture", DateTimeOffset.UnixEpoch, "review-1");tenant.MarkAsActive();
        foreach(var module in new[]{"Core","IAM"}) tenant.Modules.Add(new ST.Domain.Features.ControlPlane.Tenants.Entities.TenantModule { TenantId=id,ModuleKey=module,CreatedBy="test-fixture" });
        db.Tenants.Add(tenant);await db.SaveChangesAsync();
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

        builder.ConfigureTestServices(services =>
        {
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
                    ? TestAuthHandler.AuthenticationScheme : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
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
            RewireDbContext<ControlPlaneDBContext, ControlPlaneSqlServerDBContext>(services, _containerConnectionString);
            RewireDbContext<IamDBContext, IamSqlServerDBContext>(services, _containerConnectionString);
            RewireDbContext<SharedDBContext, SharedSqlServerDBContext>(services, _containerConnectionString);
            services.AddControllers().AddApplicationPart(typeof(ST.Tests.Integration.NeutralSlice.NeutralSliceController).Assembly);
            services.AddDbContext<ST.Tests.Integration.NeutralSlice.NeutralSliceDBContext>(options => options.UseSqlServer(_containerConnectionString));
            services.AddScoped<ST.Tests.Integration.NeutralSlice.NeutralSliceUnitOfWork>();
            services.AddTransient<MediatR.IRequestHandler<ST.Tests.Integration.NeutralSlice.CreateNeutralNote, Guid>, ST.Tests.Integration.NeutralSlice.CreateNeutralNoteHandler>();
            services.AddTransient<MediatR.IRequestHandler<ST.Tests.Integration.NeutralSlice.ReadNeutralNote, string?>, ST.Tests.Integration.NeutralSlice.ReadNeutralNoteHandler>();

        });
    }

    private static void RewireDbContext<TContext, TSqlServerContext>(
        IServiceCollection services,
        string connectionString)
        where TContext : DbContext
        where TSqlServerContext : TContext
    {
        // Remove existing DbContextOptions descriptors registered by Program.cs
        var descriptorsToRemove = services
            .Where(d =>
                d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<TContext>)
             || d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<TSqlServerContext>)
             || d.ServiceType == typeof(DbContextOptions<TContext>)
             || d.ServiceType == typeof(DbContextOptions<TSqlServerContext>))
            .ToList();
        foreach (var d in descriptorsToRemove)
            services.Remove(d);

        // Remove existing concrete context registrations
        var concreteToRemove = services
            .Where(d =>
                (d.ServiceType == typeof(TContext) && d.ImplementationType == typeof(TSqlServerContext))
             || d.ServiceType == typeof(TSqlServerContext))
            .ToList();
        foreach (var d in concreteToRemove)
            services.Remove(d);

        // Re-register pointing at the Testcontainer. No migration settings needed
        // because we use EnsureCreated / CreateTablesAsync for test schema setup.
        services.AddDbContext<TContext, TSqlServerContext>((_, options) =>
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(60)));
    }

    public async Task InitializeAsync()
    {
        // Step 1: Start the container — must happen before anything else.
        await _dbContainer.StartAsync();
        await _mailpit.StartAsync();
        var rawCs = _dbContainer.GetConnectionString();
        _containerConnectionString = rawCs.TrimEnd(';') + ";TrustServerCertificate=True;Encrypt=False;";

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
        // We catch SQL Server error 2714 (object already exists) and 1913 (index already exists)
        // to handle tables/indexes that appear in multiple context models.
        //
        // Order matters: ControlPlane first — TenantConnectionInterceptor queries [Tenants] on
        // every connection open before any other table can be touched.
        await sp.GetRequiredService<ControlPlaneDBContext>().Database.EnsureCreatedAsync();
        await CreateTablesAsync(sp.GetRequiredService<IamDBContext>());
        await CreateTablesAsync(sp.GetRequiredService<SharedDBContext>());
        await CreateTablesAsync(sp.GetRequiredService<ST.Tests.Integration.NeutralSlice.NeutralSliceDBContext>());
    }

    new public async Task DisposeAsync()
    {
        await _mailpit.DisposeAsync();
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates the tables defined in <paramref name="context"/>'s model against the
    /// already-existing test database, silently ignoring SQL Server errors when a table
    /// or index already exists (created by a previously-called context).
    /// </summary>
    private static async Task CreateTablesAsync(DbContext context)
    {
        try
        {
            var creator = context.GetService<IRelationalDatabaseCreator>();
            await creator.CreateTablesAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
            when (ex.Number is 2714 or 1913)
        {
            // 2714 = There is already an object named '…' in the database.
            // 1913 = The operation failed because an index or statistics with name '…' already exists.
            // Both are expected when contexts share a database and tables/indexes overlap.
        }
    }
}

using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Features.Banking.Customers.ValueObjects;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.Banking.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public abstract class BankingTenantIsolationTests<TFixture> : IClassFixture<TFixture> where TFixture : DbFixture
{
    private readonly DbContextOptions<BankingDBContext> _options;
    private readonly TFixture _fixture;

    protected BankingTenantIsolationTests(TFixture fixture)
    {
        _fixture = fixture;

        var builder = new DbContextOptionsBuilder<BankingDBContext>();

        if (fixture is PostgreSqlDbFixture)
        {
            builder.UseNpgsql(fixture.GetConnectionString());
        }
        else if (fixture is MsSqlDbFixture)
        {
            builder.UseSqlServer(fixture.GetConnectionString());
        }

        _options = builder.Options;
    }

    private class TestTenantProvider : ICurrentTenantProvider
    {
        public Guid TenantId { get; set; }
    }

    private class TestActorProvider : ICurrentActorProvider
    {
        public string ActorId => "TestActor";
    }

    [Fact]
    public async Task Query_ShouldOnlyReturnDataForCurrentTenant()
    {
        // Arrange
        var tenant1 = Guid.CreateVersion7();
        var tenant2 = Guid.CreateVersion7();

        var tenantProvider = new TestTenantProvider { TenantId = tenant1 };
        var actorProvider = new TestActorProvider();

        var corpDetail = CorporateDetail.Create("TestCorp", (LineOfBusiness)1, "IT", (IdentificationType)1, "123", DateTimeOffset.UtcNow, "test", "test", "test", null, null, null, null, null, null);
        var address = Address.Create("Street", "City", "State", "ZIP", "Country", "test@test.com", "1234567890");
        var commPref = CommunicationPreference.Create(true, true, true);

        // Seed data acting as tenant 1
        using (var context = new BankingDBContext(_options, tenantProvider, actorProvider))
        {
            var creator = context.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
            if (!await creator.HasTablesAsync())
            {
                if (context.Database.IsSqlServer())
                {
                    await context.Database.ExecuteSqlRawAsync("IF NOT EXISTS (SELECT * FROM sys.tables WHERE name='Employees') CREATE TABLE [Employees] ([Id] uniqueidentifier NOT NULL PRIMARY KEY)");
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM [Employees] WHERE [Id] = '0194f800-0000-7000-8000-000000000001')
                            INSERT INTO [Employees] ([Id]) VALUES ('0194f800-0000-7000-8000-000000000001'), ('0194f800-0000-7000-8000-000000000002'), ('0194f800-0000-7000-8000-000000000003')");
                }
                else
                {
                    await context.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS \"Employees\" (\"Id\" uuid NOT NULL PRIMARY KEY)");
                    await context.Database.ExecuteSqlRawAsync("INSERT INTO \"Employees\" (\"Id\") VALUES ('0194f800-0000-7000-8000-000000000001'), ('0194f800-0000-7000-8000-000000000002'), ('0194f800-0000-7000-8000-000000000003') ON CONFLICT DO NOTHING");
                }

                var script = context.Database.GenerateCreateScript();
                if (context.Database.IsSqlServer())
                {
                    var batches = script.Split(new[] { "GO\r\n", "GO\n", "GO\r", "\r\nGO", "\nGO" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var batch in batches)
                    {
                        if (!string.IsNullOrWhiteSpace(batch) && batch.Trim() != "GO")
                        {
                            await context.Database.ExecuteSqlRawAsync(batch);
                        }
                    }
                }
                else
                {
                    await context.Database.ExecuteSqlRawAsync(script);
                }
            }

            var rmId1 = Guid.CreateVersion7();
            if (context.Database.IsSqlServer())
            {
                await context.Database.ExecuteSqlRawAsync($"INSERT INTO [Employees] ([Id]) VALUES ('{rmId1}')");
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync($"INSERT INTO \"Employees\" (\"Id\") VALUES ('{rmId1}')");
            }

            var cust1 = Customer.Create("CUST1", "Customer 1", CustomerType.Corporate, SegmentType.Corporate, SubSegmentType.Local, rmId1, DateTimeOffset.UtcNow, corpDetail, address, commPref, "test");
            context.Customers.Add(cust1);
            await context.SaveChangesAsync();
        }

        // Seed data acting as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new BankingDBContext(_options, tenantProvider, actorProvider))
        {
            var rmId2 = Guid.CreateVersion7();
            if (context.Database.IsSqlServer())
            {
                await context.Database.ExecuteSqlRawAsync($"INSERT INTO [Employees] ([Id]) VALUES ('{rmId2}')");
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync($"INSERT INTO \"Employees\" (\"Id\") VALUES ('{rmId2}')");
            }

            var cust2 = Customer.Create("CUST2", "Customer 2", CustomerType.Corporate, SegmentType.Corporate, SubSegmentType.Local, rmId2, DateTimeOffset.UtcNow, corpDetail, address, commPref, "test");
            context.Customers.Add(cust2);
            await context.SaveChangesAsync();
        }

        // Act - Query as tenant 1
        tenantProvider.TenantId = tenant1;
        using (var context = new BankingDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Customers.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Number == "CUST1");
            Assert.DoesNotContain(items, i => i.Number == "CUST2");
        }

        // Act - Query as tenant 2
        tenantProvider.TenantId = tenant2;
        using (var context = new BankingDBContext(_options, tenantProvider, actorProvider))
        {
            var items = await context.Customers.ToListAsync();

            // Assert
            Assert.Contains(items, i => i.Number == "CUST2");
            Assert.DoesNotContain(items, i => i.Number == "CUST1");
        }
    }
}

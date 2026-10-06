using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ST.Persistence.Features.Banking.DataContext;

public class BankingPostgreSqlDBContextFactory : IDesignTimeDbContextFactory<BankingPostgreSqlDBContext>
{
    public BankingPostgreSqlDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<BankingPostgreSqlDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "BankingConnection", "DefaultPostgreSqlConnection");

        optionsBuilder.UseNpgsql(
            connectionString,
            pgOptions => DesignTimeConfigurationFactory.ConfigurePostgreSql(pgOptions, "__EFMigrationsHistory_Banking")).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, ST.Persistence.Features.Shared.Migrations.Generators.IdempotentNpgsqlMigrationsSqlGenerator>();
        return new BankingPostgreSqlDBContext(optionsBuilder.Options);
    }
}

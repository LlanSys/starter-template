using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ST.Persistence.Features.Banking.DataContext;

public class BankingSqlServerDBContextFactory : IDesignTimeDbContextFactory<BankingSqlServerDBContext>
{
    public BankingSqlServerDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<BankingSqlServerDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "BankingConnection");

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => DesignTimeConfigurationFactory.ConfigureSqlServer(sqlOptions, "__EFMigrationsHistory_Banking")).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, ST.Persistence.Features.Shared.Migrations.Generators.IdempotentSqlServerMigrationsSqlGenerator>();
        return new BankingSqlServerDBContext(optionsBuilder.Options);
    }
}

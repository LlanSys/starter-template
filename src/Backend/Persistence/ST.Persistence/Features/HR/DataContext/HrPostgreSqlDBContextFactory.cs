using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ST.Persistence.Features.HR.DataContext;

public class HrPostgreSqlDBContextFactory : IDesignTimeDbContextFactory<HrPostgreSqlDBContext>
{
    public HrPostgreSqlDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<HrPostgreSqlDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "HrConnection", "DefaultPostgreSqlConnection");

        optionsBuilder.UseNpgsql(
            connectionString,
            pgOptions => DesignTimeConfigurationFactory.ConfigurePostgreSql(pgOptions, "__EFMigrationsHistory_HR")).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, ST.Persistence.Features.Shared.Migrations.Generators.IdempotentNpgsqlMigrationsSqlGenerator>();
        return new HrPostgreSqlDBContext(optionsBuilder.Options);
    }
}

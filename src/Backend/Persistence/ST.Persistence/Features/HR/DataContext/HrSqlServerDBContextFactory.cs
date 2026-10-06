using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ST.Persistence.Features.HR.DataContext;

public class HrSqlServerDBContextFactory : IDesignTimeDbContextFactory<HrSqlServerDBContext>
{
    public HrSqlServerDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<HrSqlServerDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "HrConnection");

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => DesignTimeConfigurationFactory.ConfigureSqlServer(sqlOptions, "__EFMigrationsHistory_HR")).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, ST.Persistence.Features.Shared.Migrations.Generators.IdempotentSqlServerMigrationsSqlGenerator>();
        return new HrSqlServerDBContext(optionsBuilder.Options);
    }
}

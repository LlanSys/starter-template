using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using ST.Persistence.Features.Shared.Migrations.Generators;

namespace ST.Persistence.Features.ControlPlane.DataContext;

public class ControlPlaneSqlServerDBContextFactory : IDesignTimeDbContextFactory<ControlPlaneSqlServerDBContext>
{
    public ControlPlaneSqlServerDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<ControlPlaneSqlServerDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "ControlPlaneConnection", fallbackConnectionName: "DefaultSqlConnection");

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => DesignTimeConfigurationFactory.ConfigureSqlServer(sqlOptions, "__EFMigrationsHistory_ControlPlane")).ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
        return new ControlPlaneSqlServerDBContext(optionsBuilder.Options, TimeProvider.System);
    }
}

using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ST.Persistence.Features.ControlPlane.DataContext;

public class ControlPlanePostgreSqlDBContextFactory : IDesignTimeDbContextFactory<ControlPlanePostgreSqlDBContext>
{
    public ControlPlanePostgreSqlDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<ControlPlanePostgreSqlDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "ControlPlaneConnection", "DefaultPostgreSqlConnection");

        optionsBuilder.UseNpgsql(
            connectionString,
            pgOptions => DesignTimeConfigurationFactory.ConfigurePostgreSql(pgOptions, "__EFMigrationsHistory_ControlPlane")).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, ST.Persistence.Features.Shared.Migrations.Generators.IdempotentNpgsqlMigrationsSqlGenerator>();
        return new ControlPlanePostgreSqlDBContext(optionsBuilder.Options);
    }
}

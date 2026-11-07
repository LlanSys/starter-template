using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using ST.Persistence.Features.Shared.Migrations.Generators;

namespace ST.Persistence.Features.Shared.DataContext;

public class SharedPostgreSqlDBContextFactory : IDesignTimeDbContextFactory<SharedPostgreSqlDBContext>
{
    public SharedPostgreSqlDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<SharedPostgreSqlDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "SharedConnection", "DefaultPostgreSqlConnection");

        optionsBuilder.UseNpgsql(
            connectionString,
            pgOptions => DesignTimeConfigurationFactory.ConfigurePostgreSql(pgOptions, "__EFMigrationsHistory_Shared")).ReplaceService<IMigrationsSqlGenerator, IdempotentNpgsqlMigrationsSqlGenerator>();
        return new SharedPostgreSqlDBContext(optionsBuilder.Options, TimeProvider.System, null!, null!);
    }
}

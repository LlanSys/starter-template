using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ST.Persistence.Features.IAM.DataContext;

public class IamPostgreSqlDBContextFactory : IDesignTimeDbContextFactory<IamPostgreSqlDBContext>
{
    public IamPostgreSqlDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<IamPostgreSqlDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "IamConnection", "DefaultPostgreSqlConnection");

        optionsBuilder.UseNpgsql(
            connectionString,
            pgOptions => DesignTimeConfigurationFactory.ConfigurePostgreSql(pgOptions, "__EFMigrationsHistory_IAM")).ReplaceService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator, ST.Persistence.Features.Shared.Migrations.Generators.IdempotentNpgsqlMigrationsSqlGenerator>();
        return new IamPostgreSqlDBContext(optionsBuilder.Options);
    }
}

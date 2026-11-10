using ST.Persistence.Common.DesignTime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using ST.Persistence.Features.Shared.Migrations.Generators;

namespace ST.Persistence.Features.IAM.DataContext;

public class IamSqlServerDBContextFactory : IDesignTimeDbContextFactory<IamSqlServerDBContext>
{
    public IamSqlServerDBContext CreateDbContext(string[] args)
    {
        var configuration = DesignTimeConfigurationFactory.Create();
        var optionsBuilder = new DbContextOptionsBuilder<IamSqlServerDBContext>();
        var connectionString = DesignTimeConfigurationFactory.GetConnectionString(configuration, "IamConnection", fallbackConnectionName: "DefaultSqlConnection");

        optionsBuilder.UseSqlServer(
            connectionString,
            sqlOptions => DesignTimeConfigurationFactory.ConfigureSqlServer(sqlOptions, "__EFMigrationsHistory_IAM"))
            .ReplaceService<IMigrationsSqlGenerator, IdempotentSqlServerMigrationsSqlGenerator>();
        return new IamSqlServerDBContext(optionsBuilder.Options, TimeProvider.System);
    }
}

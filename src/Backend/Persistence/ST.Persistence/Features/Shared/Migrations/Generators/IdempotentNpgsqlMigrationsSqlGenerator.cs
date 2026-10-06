using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace ST.Persistence.Features.Shared.Migrations.Generators;

#pragma warning disable EF1001 // Internal EF Core API usage.
public class IdempotentNpgsqlMigrationsSqlGenerator : NpgsqlMigrationsSqlGenerator
{
    public IdempotentNpgsqlMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        INpgsqlSingletonOptions npgsqlSingletonOptions)
        : base(dependencies, npgsqlSingletonOptions)
    {
    }

    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        var dummyBuilder = new MigrationCommandListBuilder(Dependencies);
        base.Generate(operation, model, dummyBuilder, terminate: false);
        dummyBuilder.EndCommand();

        var command = dummyBuilder.GetCommandList().FirstOrDefault();
        if (command != null)
        {
            var sql = command.CommandText;
            sql = Regex.Replace(sql, @"^CREATE\s+(UNIQUE\s+)?INDEX", "CREATE $1INDEX IF NOT EXISTS", RegexOptions.IgnoreCase);

            builder.Append(sql);
        }

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }
    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        var dummyBuilder = new MigrationCommandListBuilder(Dependencies);
        base.Generate(operation, model, dummyBuilder, terminate: false);
        dummyBuilder.EndCommand();

        var command = dummyBuilder.GetCommandList().FirstOrDefault();
        if (command != null)
        {
            var sql = command.CommandText;
            sql = Regex.Replace(sql, @"^CREATE\s+TABLE", "CREATE TABLE IF NOT EXISTS", RegexOptions.IgnoreCase);

            builder.Append(sql);
        }

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }
}
#pragma warning restore EF1001 // Internal EF Core API usage.

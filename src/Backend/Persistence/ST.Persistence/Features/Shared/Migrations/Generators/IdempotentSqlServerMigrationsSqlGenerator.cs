using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using Microsoft.EntityFrameworkCore.Update;

namespace ST.Persistence.Features.Shared.Migrations.Generators;

public class IdempotentSqlServerMigrationsSqlGenerator : SqlServerMigrationsSqlGenerator
{
#pragma warning disable EF1001 // Internal EF Core API usage.
    public IdempotentSqlServerMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        ICommandBatchPreparer commandBatchPreparer)
        : base(dependencies, commandBatchPreparer)
    {
    }
#pragma warning restore EF1001 // Internal EF Core API usage.

    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        var schema = operation.Schema;
        var table = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, schema);

        builder
            .Append($"IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = '{operation.Name}' AND object_id = OBJECT_ID('{table}'))")
            .AppendLine()
            .Append("BEGIN")
            .AppendLine();

        using (builder.Indent())
        {
            base.Generate(operation, model, builder, terminate: false);
            builder.AppendLine(";");
        }

        builder
            .Append("END")
            .AppendLine()
            .Append("ELSE")
            .AppendLine()
            .Append("BEGIN")
            .AppendLine()
            .Append($"    PRINT 'Index {operation.Name} on table {table} already exists. Skipping creation.';")
            .AppendLine()
            .Append("END")
            .AppendLine();

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }
    protected override void Generate(CreateTableOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        var schema = operation.Schema;
        var table = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, schema);
        var schemaCondition = schema != null ? $" AND schema_id = SCHEMA_ID('{schema}')" : "";

        builder
            .Append($"IF NOT EXISTS(SELECT * FROM sys.tables WHERE name = '{operation.Name}'{schemaCondition})")
            .AppendLine()
            .Append("BEGIN")
            .AppendLine();

        using (builder.Indent())
        {
            base.Generate(operation, model, builder, terminate: false);
        }

        builder
            .AppendLine()
            .Append("END")
            .AppendLine()
            .Append("ELSE")
            .AppendLine()
            .Append("BEGIN")
            .AppendLine()
            .Append($"    PRINT 'Table {table} already exists. Skipping creation.';")
            .AppendLine()
            .Append("END")
            .AppendLine();

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }

    protected override void Generate(InsertDataOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        var schema = operation.Schema;
        var table = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, schema);

        builder.AppendLine("BEGIN TRY");
        using (builder.Indent())
        {
            base.Generate(operation, model, builder, terminate: false);
            builder.AppendLine(";");
        }
        builder.AppendLine("END TRY");
        builder.AppendLine("BEGIN CATCH");
        using (builder.Indent())
        {
            builder.AppendLine("PRINT ERROR_MESSAGE();");
            builder.AppendLine($"PRINT 'Ignoring error during data insertion for {table} (likely idempotent).';");
            builder.AppendLine($"IF OBJECTPROPERTY(OBJECT_ID('{table}'), 'TableHasIdentity') = 1");
            builder.AppendLine($"BEGIN");
            builder.AppendLine($"    SET IDENTITY_INSERT {table} OFF;");
            builder.AppendLine($"END");
        }
        builder.AppendLine("END CATCH");

        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
            builder.EndCommand();
        }
    }
}

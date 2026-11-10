using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.Persistence.Features.Shared.DataContext;

namespace ST.DatabaseTools;

internal static class LocalMigrationWorkflow
{
    public static async Task<bool> RunAsync(
        LocalDatabaseTarget target,
        TimeProvider clock,
        bool apply,
        CancellationToken cancellationToken)
    {
        ControlPlaneDBContext controlPlane = target.SqlServer
            ? new ControlPlaneSqlServerDBContext(target.CreateOptions<ControlPlaneSqlServerDBContext>("ControlPlane"), clock)
            : new ControlPlanePostgreSqlDBContext(target.CreateOptions<ControlPlanePostgreSqlDBContext>("ControlPlane"), clock);
        await using var controlPlaneLifetime = controlPlane.ConfigureAwait(false);

        var iam = target.CreateIamContext(clock);
        await using var iamLifetime = iam.ConfigureAwait(false);

        var scope = new MigrationScope();
        SharedDBContext shared = target.SqlServer
            ? new SharedSqlServerDBContext(target.CreateOptions<SharedSqlServerDBContext>("Shared"), clock, scope, scope)
            : new SharedPostgreSqlDBContext(target.CreateOptions<SharedPostgreSqlDBContext>("Shared"), clock, scope, scope);
        await using var sharedLifetime = shared.ConfigureAwait(false);

        var connection = controlPlane.Database.GetDbConnection();
        Console.WriteLine($"Local database '{connection.Database}' on '{connection.DataSource}'.");

        var databaseCreator = controlPlane.GetService<IRelationalDatabaseCreator>();
        if (!await databaseCreator.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new LocalDatabaseOperationException("The local database is missing. Use the documented fresh database setup.");
        }

        DbContext[] contexts = [controlPlane, iam, shared];
        var pendingContexts = await ReportAsync(contexts, cancellationToken).ConfigureAwait(false);
        if (pendingContexts is null)
        {
            return false;
        }

        if (!apply || pendingContexts.Count == 0)
        {
            return true;
        }

        foreach (var context in pendingContexts)
        {
            Console.WriteLine($"Applying migrations for {context.GetType().Name}.");
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        var remaining = await ReportAsync(contexts, cancellationToken).ConfigureAwait(false);
        return remaining is { Count: 0 };
    }

    private static async Task<List<DbContext>?> ReportAsync(DbContext[] contexts, CancellationToken cancellationToken)
    {
        var valid = true;
        var pendingContexts = new List<DbContext>();
        foreach (var context in contexts)
        {
            var pending = await CheckContextAsync(context, cancellationToken).ConfigureAwait(false);
            if (pending is null)
            {
                valid = false;
            }
            else if (pending.Length > 0)
            {
                pendingContexts.Add(context);
            }
        }

        return valid ? pendingContexts : null;
    }

    private static async Task<string[]?> CheckContextAsync(DbContext context, CancellationToken cancellationToken)
    {
        var name = context.GetType().Name;
        var known = context.Database.GetMigrations().ToArray();
        var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false))
            .ToHashSet(StringComparer.Ordinal);

        if (known.Length == 0 || !applied.Contains(known[0]))
        {
            Console.Error.WriteLine($"{name}: this checkout's initial migration is missing from the local history. Verify the target and use the documented fresh setup for a new database.");
            return null;
        }

        var unknown = applied.Except(known, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            Console.Error.WriteLine($"{name}: migrations absent from this checkout: {string.Join(", ", unknown)}.");
            return null;
        }

        var firstGap = known.Take(applied.Count).FirstOrDefault(migration => !applied.Contains(migration));
        if (firstGap is not null)
        {
            Console.Error.WriteLine($"{name}: the history skips '{firstGap}' before a later applied migration.");
            return null;
        }

        if (context.GetService<IMigrationsAssembly>().ModelSnapshot is null || context.Database.HasPendingModelChanges())
        {
            Console.Error.WriteLine($"{name}: the model differs from its migration snapshot. Scaffold and review migrations for both providers.");
            return null;
        }

        var pending = known.Skip(applied.Count).ToArray();
        Console.WriteLine($"{name}: {pending.Length} pending migrations.");
        foreach (var migration in pending)
        {
            Console.WriteLine($"  {migration}");
        }

        return pending;
    }

    private sealed class MigrationScope : ICurrentTenantProvider, ICurrentActorProvider
    {
        public Guid TenantId => Guid.Empty;
        public string ActorId => ICurrentActorProvider.SystemActor;
    }
}

using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace ST.DatabaseTools;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args is not ["SqlServer" or "PostgreSql",
            "local-status" or "local-upgrade" or "check-local-authenticator" or "reprotect-local-authenticator"])
        {
            Console.Error.WriteLine("Usage: ST.DatabaseTools SqlServer|PostgreSql local-status|local-upgrade|check-local-authenticator|reprotect-local-authenticator");
            return 2;
        }

        var sqlServer = args[0] == "SqlServer";
        var operation = args[1];
        var isMigrationOperation = operation is "local-status" or "local-upgrade";

        if (!isMigrationOperation && !OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Run this operation as the original Windows user with access to the original DPAPI keys.");
            return 2;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            var clock = TimeProvider.System;
            if (isMigrationOperation)
            {
                var target = LocalDatabaseTarget.ResolveForMigrations(sqlServer);
                var valid = await LocalMigrationWorkflow.RunAsync(
                    target, clock, apply: operation == "local-upgrade", cancellation.Token).ConfigureAwait(false);
                return valid ? 0 : 1;
            }

            var recoveryTarget = new LocalDatabaseTarget(sqlServer, RequiredSetting("ST_DATABASE_CONNECTION"));
            var context = recoveryTarget.CreateIamContext(clock);
            await using var contextLifetime = context.ConfigureAwait(false);
            await LocalAuthenticatorReprotection.RunAsync(
                context, clock, apply: operation == "reprotect-local-authenticator", cancellation.Token)
                .ConfigureAwait(false);

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Local database maintenance was cancelled.");
        }
        catch (CryptographicException) when (!isMigrationOperation)
        {
            Console.Error.WriteLine("The original or Redis key ring could not recover this enrollment. Check the key stores.");
        }
        catch (LocalDatabaseOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
        }
        catch (SqlException exception) when (isMigrationOperation)
        {
            Console.Error.WriteLine($"Local SQL Server operation failed (diagnostic code {exception.Number}).");
        }
        catch (PostgresException exception) when (isMigrationOperation)
        {
            Console.Error.WriteLine($"Local PostgreSQL operation failed (diagnostic code {exception.SqlState}).");
        }
        catch (Exception)
        {
            Console.Error.WriteLine(isMigrationOperation
                ? "Local migration workflow failed. Check the target, provider, and the last context reported above."
                : "Authenticator maintenance failed. Check database, Redis, and account configuration.");
        }

        return 1;
    }

    internal static string RequiredSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new LocalDatabaseOperationException($"Set {name} in the process environment.");
    }
}

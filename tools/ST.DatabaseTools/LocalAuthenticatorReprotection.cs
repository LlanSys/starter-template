using System.Data;
using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using ST.Persistence.Features.IAM.DataContext;

namespace ST.DatabaseTools;

internal static class LocalAuthenticatorReprotection
{
    public static async Task RunAsync(IamDBContext context, TimeProvider clock, bool apply, CancellationToken cancellationToken)
    {
        var userId = Program.RequiredSetting("ST_AUTHENTICATOR_USER_ID");
        var sourceDirectory = new DirectoryInfo(Program.RequiredSetting("ST_LOCAL_SOURCE_KEY_DIRECTORY"));
        var redisOptions = ConfigurationOptions.Parse(Program.RequiredSetting("ST_LOCAL_REDIS_CONNECTION"));

        if (redisOptions.EndPoints.Count == 0 || redisOptions.EndPoints.Any(endpoint => endpoint switch
            {
                DnsEndPoint dns => !LocalDatabaseTarget.IsLoopback(dns.Host),
                IPEndPoint ip => !IPAddress.IsLoopback(ip.Address),
                _ => true
            }))
        {
            throw new LocalDatabaseOperationException("The target must be the existing loopback Redis key store.");
        }

        if (!sourceDirectory.Exists)
        {
            throw new LocalDatabaseOperationException("The original Windows key directory is unavailable.");
        }

        const string applicationName = "StarterTemplate";
        const string ringKey = "StarterTemplate-DataProtection-Keys";
        const string purpose = "TotpSecrets";

        var source = DataProtectionProvider.Create(sourceDirectory, builder =>
            builder.SetApplicationName(applicationName).DisableAutomaticKeyGeneration()).CreateProtector(purpose);

        var redis = await ConnectionMultiplexer.ConnectAsync(redisOptions).ConfigureAwait(false);
        await using var redisLifetime = redis.ConfigureAwait(false);
        var services = new ServiceCollection().AddLogging();
        services.AddDataProtection()
            .SetApplicationName(applicationName)
            .DisableAutomaticKeyGeneration()
            .PersistKeysToStackExchangeRedis(redis, ringKey);

        var provider = services.BuildServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var target = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector(purpose);

        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
            .ConfigureAwait(false);
        await using var transactionLifetime = transaction.ConfigureAwait(false);
        // This local operation selects exactly one globally unique IAM account across tenant filters.
        var user = await context.Users.IgnoreQueryFilters()
            .SingleOrDefaultAsync(user => user.Id == userId && !user.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (user is null || !user.TwoFactorEnabled)
        {
            throw new LocalDatabaseOperationException("The selected account has no enabled authenticator to preserve.");
        }

        var now = clock.GetUtcNow();
        var active = await context.AppUserTotpSecrets.IgnoreQueryFilters()
            .Where(secret => secret.AppUserId == userId && secret.IsActive && !secret.IsDeleted)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (active.Count == 0)
        {
            throw new LocalDatabaseOperationException("The selected account has no active authenticator record.");
        }

        var pending = await context.TempTotpSecrets.IgnoreQueryFilters()
            .Where(secret => secret.UserId == userId && !secret.IsDeleted && secret.ExpiresAt > now)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var changed = 0;
        string Reprotect(string encrypted)
        {
            try
            {
                _ = target.Unprotect(encrypted);
                return encrypted;
            }
            catch (CryptographicException)
            {
                // Existing Windows ciphertext needs the source protector.
            }

            var plaintext = source.Unprotect(encrypted);
            var replacement = target.Protect(plaintext);

            if (!string.Equals(plaintext, target.Unprotect(replacement), StringComparison.Ordinal))
            {
                throw new CryptographicException("Authenticator continuity validation failed.");
            }

            changed++;
            return replacement;
        }

        foreach (var secret in active)
        {
            context.Entry(secret).Property(record => record.EncryptedSecret).CurrentValue = Reprotect(secret.EncryptedSecret);
        }

        foreach (var secret in pending)
        {
            context.Entry(secret).Property(record => record.EncryptedSecret).CurrentValue = Reprotect(secret.EncryptedSecret);
        }

        if (!string.IsNullOrWhiteSpace(user.TotpSecret))
        {
            user.TotpSecret = Reprotect(user.TotpSecret);
        }

        if (!apply)
        {
            Console.WriteLine($"Checked the authenticator against both key stores. Records requiring reprotection: {changed}.");
            return;
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"Reprotected authenticator records: {changed}. The existing enrollment and MFA remain enabled.");
    }
}

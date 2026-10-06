using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Logging;

internal static partial class PersistenceLogDefinitions
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Entity {EntityTypeName} with ID {EntityId} created by {User}")]
    public static partial void LogEntityCreated(ILogger logger, string entityTypeName, Guid entityId, string user);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Entity {EntityTypeName} with ID {EntityId} updated by {User}")]
    public static partial void LogEntityUpdated(ILogger logger, string entityTypeName, Guid entityId, string user);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Information, Message = "Entity {EntityTypeName} with ID {EntityId} was soft-deleted by {User}")]
    public static partial void LogSoftDelete(ILogger logger, string entityTypeName, Guid entityId, string user);

    [LoggerMessage(EventId = 4100, Level = LogLevel.Error, Message = "Concurrency conflict on {EntityTypeName} (ID: {EntityId})")]
    public static partial void LogConcurrencyConflict(ILogger logger, string entityTypeName, string entityId);

    [LoggerMessage(EventId = 4101, Level = LogLevel.Error, Message = "Database operation failed for {EntityType}")]
    public static partial void LogDatabaseError(ILogger logger, string entityType, Exception ex);

    [LoggerMessage(EventId = 4200, Level = LogLevel.Debug, Message = "Executing SQL: {SqlCommand}")]
    public static partial void LogSqlExecution(ILogger logger, string sqlCommand);

    [LoggerMessage(EventId = 4201, Level = LogLevel.Warning, Message = "Concurrency conflict detected while executing transaction. Rolling back and clearing change tracker.")]
    public static partial void LogTransactionConcurrencyRollback(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4202, Level = LogLevel.Error, Message = "Error in transaction. Rolling back...")]
    public static partial void LogTransactionErrorRollback(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4203, Level = LogLevel.Debug, Message = "Successfully saved and published {EventCount} domain events")]
    public static partial void LogEventsPublished(ILogger logger, int eventCount);

    [LoggerMessage(EventId = 4204, Level = LogLevel.Error, Message = "Transaction failed - rolling back")]
    public static partial void LogCompleteWithEventsRollback(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4205, Level = LogLevel.Warning, Message = "Concurrency conflict on transaction attempt {Attempt} of {MaxRetries}; retrying.")]
    public static partial void LogTransactionConcurrencyRetry(ILogger logger, int attempt, int maxRetries, Exception ex);

    [LoggerMessage(EventId = 4206, Level = LogLevel.Error, Message = "Error in retryable transaction. Rolling back...")]
    public static partial void LogRetryableTransactionErrorRollback(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4207, Level = LogLevel.Error, Message = "Unhandled error while saving changes in {ContextName}")]
    public static partial void LogDBContextSaveChangesError(ILogger logger, string contextName, Exception ex);

    [LoggerMessage(EventId = 4208, Level = LogLevel.Error, Message = "Failed to delete expired temp TOTP secrets")]
    public static partial void LogDeleteExpiredTempTotpSecretsError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 4209, Level = LogLevel.Error, Message = "Failed to delete temp TOTP secrets for user {UserId}")]
    public static partial void LogDeleteUserTempTotpSecretsError(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 4210, Level = LogLevel.Error, Message = "Failed to deactivate TOTP secrets for user {UserId}")]
    public static partial void LogDeactivateUserTotpSecretsError(ILogger logger, string userId, Exception ex);
}

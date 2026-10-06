using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Utilities;

internal static partial class LogDefinitions
{
    [LoggerMessage(EventId = 2300, Level = LogLevel.Error, Message = "MediatR Pipeline caught exception for {RequestName}")]
    public static partial void LogPipelineException(ILogger logger, string requestName, Exception ex);

    [LoggerMessage(EventId = 2301, Level = LogLevel.Error, Message = "An unexpected error occurred: {ErrorMessage}")]
    public static partial void LogUnexpectedError(ILogger logger, string errorMessage, Exception exception);

    [LoggerMessage(EventId = 2302, Level = LogLevel.Information, Message = "Customer created: {Number} — {CompanyName}")]
    public static partial void LogCustomerCreated(ILogger logger, string number, string companyName);

    [LoggerMessage(EventId = 2303, Level = LogLevel.Warning, Message = "Domain validation failed creating customer")]
    public static partial void LogCustomerCreateValidationFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2304, Level = LogLevel.Error, Message = "Error creating customer")]
    public static partial void LogCustomerCreateFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2305, Level = LogLevel.Information, Message = "Customer updated: {Number}")]
    public static partial void LogCustomerUpdated(ILogger logger, string number);

    [LoggerMessage(EventId = 2306, Level = LogLevel.Error, Message = "Error updating customer {CustomerId}")]
    public static partial void LogCustomerUpdateFailed(ILogger logger, Guid customerId, Exception ex);

    [LoggerMessage(EventId = 2307, Level = LogLevel.Error, Message = "Error deleting customer {CustomerId}")]
    public static partial void LogCustomerDeleteFailed(ILogger logger, Guid customerId, Exception ex);

    [LoggerMessage(EventId = 2308, Level = LogLevel.Information, Message = "Successfully published CustomerCreatedIntegrationEvent for customer {CustomerId}")]
    public static partial void LogCustomerCreatedIntegrationPublished(ILogger logger, Guid customerId);

    [LoggerMessage(EventId = 2309, Level = LogLevel.Error, Message = "Error occurred while handling CustomerCreatedDomainEvent for MemberEmail: {MemberEmail}")]
    public static partial void LogCustomerCreatedDomainEventHandlerError(ILogger logger, string memberEmail, Exception ex);

    [LoggerMessage(EventId = 2310, Level = LogLevel.Error, Message = "Error consuming CustomerCreatedIntegrationEvent for CustomerId: {CustomerId}")]
    public static partial void LogCustomerCreatedIntegrationConsumeError(ILogger logger, Guid customerId, Exception ex);

    [LoggerMessage(EventId = 2311, Level = LogLevel.Error, Message = "Email template '{TemplateName}' not found in database")]
    public static partial void LogEmailTemplateNotFound(ILogger logger, string templateName);

    [LoggerMessage(EventId = 2312, Level = LogLevel.Error, Message = "Email template mismatch: expected '{Expected}', found '{Actual}'")]
    public static partial void LogEmailTemplateMismatch(ILogger logger, string expected, string actual);

    [LoggerMessage(EventId = 2313, Level = LogLevel.Critical, Message = "Permanent failure after {Attempt} attempts - moving to dead letter")]
    public static partial void LogPermanentConsumerFailure(ILogger logger, int attempt, Exception ex);

    [LoggerMessage(EventId = 2314, Level = LogLevel.Warning, Message = "Temporary failure (attempt {Attempt}) - will retry")]
    public static partial void LogTemporaryConsumerFailure(ILogger logger, int attempt, Exception ex);

    [LoggerMessage(EventId = 2315, Level = LogLevel.Error, Message = "Error fetching customer {CustomerId}")]
    public static partial void LogGetCustomerByIdFailed(ILogger logger, Guid customerId, Exception ex);

    [LoggerMessage(EventId = 2316, Level = LogLevel.Error, Message = "Error fetching customer list")]
    public static partial void LogCustomerListFetchFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2317, Level = LogLevel.Warning, Message = "Failed to deliver OTP email for {Purpose} to {UserId}: {Message}")]
    public static partial void LogEmailOtpFailedToDeliver(ILogger logger, string purpose, string userId, string message);
    [LoggerMessage(EventId = 2317, Level = LogLevel.Error, Message = "Error fetching dashboard summary")]
    public static partial void LogDashboardSummaryFetchFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2318, Level = LogLevel.Error, Message = "Error fetching staff members")]
    public static partial void LogStaffMembersFetchFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2319, Level = LogLevel.Information, Message = "AppUserCreatedIntegrationEvent published for {UserId}")]
    public static partial void LogAppUserCreatedIntegrationPublished(ILogger logger, string userId);

    [LoggerMessage(EventId = 2320, Level = LogLevel.Error, Message = "Failed to publish integration event for AppUser {UserId}")]
    public static partial void LogAppUserCreatedIntegrationPublishFailed(ILogger logger, string userId, Exception ex);

    [LoggerMessage(EventId = 2321, Level = LogLevel.Information, Message = "Email OTP delivered for {Purpose} to {UserId}")]
    public static partial void LogEmailOtpDelivered(ILogger logger, string purpose, string userId);

    [LoggerMessage(EventId = 2322, Level = LogLevel.Warning, Message = "Duplicate registration attempt for employee: {Number}")]
    public static partial void LogEmployeeDuplicateRegistration(ILogger logger, string number);

    [LoggerMessage(EventId = 2323, Level = LogLevel.Error, Message = "Registration failed for employee {Email}. Rolling back changes.")]
    public static partial void LogEmployeeRegistrationFailed(ILogger logger, string email, Exception ex);

    [LoggerMessage(EventId = 2324, Level = LogLevel.Information, Message = "PaymentEventConsumer: Received PaymentCompletedEvent for CustomerReference: {CustomerReference}, Amount: {Amount} {Currency}")]
    public static partial void LogPaymentCompleted(ILogger logger, string customerReference, decimal amount, string currency);

    [LoggerMessage(EventId = 2325, Level = LogLevel.Warning, Message = "PaymentEventConsumer: Received PaymentFailedEvent for CustomerReference: {CustomerReference}, Reason: {FailureReason}")]
    public static partial void LogPaymentFailed(ILogger logger, string customerReference, string failureReason);

    [LoggerMessage(EventId = 2333, Level = LogLevel.Information, Message = "PaymentEventConsumer: Received PaymentCancelledEvent for CustomerReference: {CustomerReference}, Reason: {Reason}")]
    public static partial void LogPaymentCancelled(ILogger logger, string customerReference, string reason);

    // Mpesa STK Callback
    [LoggerMessage(EventId = 2326, Level = LogLevel.Warning, Message = "Invalid M-Pesa STK callback payload received.")]
    public static partial void LogMpesaStkInvalidPayload(ILogger logger);

    [LoggerMessage(EventId = 2327, Level = LogLevel.Warning, Message = "M-Pesa STK Callback received for unknown CheckoutRequestID: {CheckoutRequestId}")]
    public static partial void LogMpesaStkUnknownCheckoutRequestId(ILogger logger, string checkoutRequestId);

    [LoggerMessage(EventId = 2328, Level = LogLevel.Error, Message = "Error processing M-Pesa STK callback.")]
    public static partial void LogMpesaStkCallbackProcessingError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2329, Level = LogLevel.Information, Message = "M-Pesa STK Callback processed successfully for CheckoutRequestID: {CheckoutRequestId} with ResultCode: {ResultCode}")]
    public static partial void LogMpesaStkCallbackProcessed(ILogger logger, string checkoutRequestId, int resultCode);

    // Mpesa C2B Confirmation
    [LoggerMessage(EventId = 2330, Level = LogLevel.Information, Message = "M-Pesa C2B Confirmation received for TransID: {TransId}, BillRefNumber: {BillRefNumber}")]
    public static partial void LogMpesaC2bConfirmationReceived(ILogger logger, string transId, string billRefNumber);

    [LoggerMessage(EventId = 2331, Level = LogLevel.Warning, Message = "M-Pesa C2B Confirmation missing essential fields.")]
    public static partial void LogMpesaC2bInvalidPayload(ILogger logger);

    [LoggerMessage(EventId = 2332, Level = LogLevel.Error, Message = "Error processing M-Pesa C2B confirmation.")]
    public static partial void LogMpesaC2bConfirmationError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2334, Level = LogLevel.Information, Message = "User {ActorId} started impersonating Tenant {TenantId} until {ExpiryTime}. Reason: {Reason}")]
    public static partial void LogImpersonationStarted(ILogger logger, string actorId, Guid tenantId, DateTimeOffset expiryTime, string reason);

    [LoggerMessage(EventId = 2335, Level = LogLevel.Information, Message = "User {ActorId} ended impersonation session {RecordId}")]
    public static partial void LogImpersonationEnded(ILogger logger, string actorId, Guid recordId);
}

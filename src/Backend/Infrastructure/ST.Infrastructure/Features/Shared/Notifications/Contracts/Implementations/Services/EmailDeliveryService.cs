using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Azure.Communication.Email;
using Azure;

namespace ST.Infrastructure.Features.Shared.Notifications.Contracts.Implementations.Services;

internal sealed class EmailDeliveryService(
    IOptions<EmailSettings> options,
    IHttpClientFactory httpClientFactory,
    IHostEnvironment environment,
    IServiceProvider serviceProvider,
    ILogger<EmailDeliveryService> logger) : IEmailService
{
    private readonly EmailSettings _settings = options.Value;

    public async Task<AppResponse<SendEmailResponse>> SendEmailAsync(SendEmailRequest sendEmailRequest, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(sendEmailRequest);

            if (string.IsNullOrWhiteSpace(sendEmailRequest.To))
            {
                return AppResponses.Failure<SendEmailResponse>("Recipient email is required.");
            }

            if (!MailboxAddress.TryParse(sendEmailRequest.To, out var toAddress))
            {
                return AppResponses.Failure<SendEmailResponse>("The recipient email address is invalid.");
            }

            return GetEmailProvider(_settings) switch
            {
                EmailProvider.NoOp => SendNoOp(sendEmailRequest),
                EmailProvider.LocalMailpit => await SendToMailpitAsync(sendEmailRequest, toAddress, cancellationToken).ConfigureAwait(false),
                EmailProvider.SendGrid => await SendViaSendGridAsync(sendEmailRequest, toAddress, cancellationToken).ConfigureAwait(false),
                EmailProvider.AzureCommunication => await SendViaAcsAsync(sendEmailRequest, toAddress, cancellationToken).ConfigureAwait(false),
                EmailProvider.Resend => await SendViaResendAsync(sendEmailRequest, toAddress, cancellationToken).ConfigureAwait(false),
                _ => AppResponses.Failure<SendEmailResponse>(
                    $"Email provider '{_settings.Provider}' is not supported.")
            };
        }
        catch (Exception ex)
        {
            ServiceLogDefinitions.LogErrorSendingEmail(logger, sendEmailRequest?.To ?? string.Empty, ex);
            return AppResponses.Failure<SendEmailResponse>("Email could not be sent. Please verify email delivery configuration and try again.");
        }
    }

    private AppResponse<SendEmailResponse> SendNoOp(SendEmailRequest request)
    {
        if (environment.IsProduction() && !_settings.AllowNoOpInProduction)
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email delivery is not configured for this environment.");
        }

        var messageId = $"noop-{Guid.CreateVersion7():N}";
        ServiceLogDefinitions.LogEmailSent(logger, request.To ?? string.Empty);

        return AppResponses.Success("Email queued successfully.",
            CreateResponse(messageId, request));
    }

    private async Task<AppResponse<SendEmailResponse>> SendToMailpitAsync(
        SendEmailRequest request,
        MailboxAddress toAddress,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Local Mailpit email delivery is only available in Development.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress) ||
            !MailboxAddress.TryParse(_settings.FromAddress, out var fromAddress))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email sender address is not configured correctly.");
        }

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.DisplayName, fromAddress.Address));
        message.To.Add(toAddress);
        message.Subject = request.Subject ?? string.Empty;
        message.Body = new BodyBuilder
        {
            HtmlBody = request.Body ?? string.Empty
        }.ToMessageBody();

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(
            _settings.LocalMailpit.Host,
            _settings.LocalMailpit.Port,
            MailKit.Security.SecureSocketOptions.None,
            cancellationToken).ConfigureAwait(false);

        var messageId = await smtpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        await smtpClient.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

        ServiceLogDefinitions.LogEmailSent(logger, request.To ?? string.Empty);

        return AppResponses.Success("Email queued successfully.",
            CreateResponse(
                string.IsNullOrWhiteSpace(messageId) ? Guid.CreateVersion7().ToString() : messageId,
                request));
    }

    private async Task<AppResponse<SendEmailResponse>> SendViaSendGridAsync(
        SendEmailRequest request,
        MailboxAddress toAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SendGrid.ApiKey))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email delivery is not configured for this environment.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress) ||
            !MailboxAddress.TryParse(_settings.FromAddress, out var fromAddress))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email sender address is not configured correctly.");
        }

        using var httpClient = httpClientFactory.CreateClient("Email.SendGrid");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.SendGrid.ApiKey);

        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[] { new { email = toAddress.Address, name = toAddress.Name } },
                    subject = request.Subject ?? string.Empty
                }
            },
            from = new { email = fromAddress.Address, name = _settings.DisplayName },
            content = new[]
            {
                new { type = "text/html", value = request.Body ?? string.Empty }
            }
        };

        using var response = await httpClient
            .PostAsJsonAsync(string.Empty, payload, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ServiceLogDefinitions.LogSendGridRejection(logger, response.StatusCode, content);
            return AppResponses.Failure<SendEmailResponse>(
                "Email could not be accepted by the delivery provider.");
        }

        var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
            ? values.FirstOrDefault()
            : null;

        ServiceLogDefinitions.LogEmailSent(logger, request.To ?? string.Empty);

        return AppResponses.Success("Email queued successfully.",
            CreateResponse(
                string.IsNullOrWhiteSpace(messageId) ? Guid.CreateVersion7().ToString() : messageId,
                request));
    }

    private async Task<AppResponse<SendEmailResponse>> SendViaAcsAsync(
        SendEmailRequest request,
        MailboxAddress toAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.AzureCommunication.ConnectionString))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email delivery is not configured for this environment.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email sender address is not configured correctly.");
        }

        var emailClient = serviceProvider.GetService<EmailClient>();
        if (emailClient == null)
        {
             return AppResponses.Failure<SendEmailResponse>("EmailClient is not registered.");
        }

        var emailMessage = new EmailMessage(
            senderAddress: _settings.FromAddress,
            content: new EmailContent(request.Subject ?? string.Empty)
            {
                Html = request.Body ?? string.Empty
            },
            recipients: new EmailRecipients(new List<EmailAddress> { new EmailAddress(toAddress.Address, toAddress.Name) }));

        try
        {
            var emailSendOperation = await emailClient.SendAsync(WaitUntil.Completed, emailMessage, cancellationToken).ConfigureAwait(false);
            ServiceLogDefinitions.LogEmailSent(logger, request.To ?? string.Empty);
            return AppResponses.Success("Email queued successfully.", CreateResponse(emailSendOperation.Id ?? Guid.CreateVersion7().ToString(), request));
        }
        catch (RequestFailedException ex)
        {
            ServiceLogDefinitions.LogAcsRejection(logger, ex.Status, ex.Message);
            return AppResponses.Failure<SendEmailResponse>("Email could not be accepted by the delivery provider.");
        }
    }

    private async Task<AppResponse<SendEmailResponse>> SendViaResendAsync(
        SendEmailRequest request,
        MailboxAddress toAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Resend.ApiKey))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email delivery is not configured for this environment.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromAddress) ||
            !MailboxAddress.TryParse(_settings.FromAddress, out var fromAddress))
        {
            return AppResponses.Failure<SendEmailResponse>(
                "Email sender address is not configured correctly.");
        }

        using var httpClient = httpClientFactory.CreateClient("Email.Resend");
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.Resend.ApiKey);

        var payload = new
        {
            from = $"{_settings.DisplayName} <{fromAddress.Address}>",
            to = new[] { toAddress.Address },
            subject = request.Subject ?? string.Empty,
            html = request.Body ?? string.Empty
        };

        using var response = await httpClient
            .PostAsJsonAsync(string.Empty, payload, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            ServiceLogDefinitions.LogResendRejection(logger, response.StatusCode, content);
            return AppResponses.Failure<SendEmailResponse>(
                "Email could not be accepted by the delivery provider.");
        }

        var responseContent = await response.Content.ReadFromJsonAsync<ResendResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        var messageId = responseContent?.Id;

        ServiceLogDefinitions.LogEmailSent(logger, request.To ?? string.Empty);

        return AppResponses.Success("Email queued successfully.",
            CreateResponse(
                string.IsNullOrWhiteSpace(messageId) ? Guid.CreateVersion7().ToString() : messageId,
                request));
    }

    private sealed record ResendResponse(string Id);

    private static SendEmailResponse CreateResponse(string messageId, SendEmailRequest request) =>
        new(
            messageId,
            DateTimeOffset.UtcNow,
            request.To ?? string.Empty,
            request.Subject ?? string.Empty);

    private static EmailProvider GetEmailProvider(EmailSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Provider))
        {
            return EmailProvider.NoOp;
        }

        return settings.Provider.Trim() switch
        {
            var provider when provider.Equals("NoOp", StringComparison.OrdinalIgnoreCase) => EmailProvider.NoOp,
            var provider when provider.Equals("LocalMailpit", StringComparison.OrdinalIgnoreCase) => EmailProvider.LocalMailpit,
            var provider when provider.Equals("SendGrid", StringComparison.OrdinalIgnoreCase) => EmailProvider.SendGrid,
            var provider when provider.Equals("AzureCommunication", StringComparison.OrdinalIgnoreCase) => EmailProvider.AzureCommunication,
            var provider when provider.Equals("Resend", StringComparison.OrdinalIgnoreCase) => EmailProvider.Resend,
            _ => EmailProvider.Invalid
        };
    }

    private enum EmailProvider
    {
        NoOp,
        LocalMailpit,
        SendGrid,
        AzureCommunication,
        Resend,
        Invalid
    }
}

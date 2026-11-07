using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Application.Features.Shared.Notifications.Contracts.Interfaces;
using ST.Infrastructure.Configuration;
using ST.Infrastructure.Contracts.Interfaces;
using ST.Infrastructure.Logging;
using ST.SharedKernel.Dtos.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ST.Infrastructure.Features.Shared.Notifications.Contracts.Implementations.Services;

internal sealed class SmsService(
    IOptions<SmsSettings> smsSettings,
    IApiService apiService,
    IHttpClientFactory httpClientFactory,
    ILogger<SmsService> logger

) : ISmsService
{
    private readonly SmsSettings _smsSettings = smsSettings.Value;
    private readonly IApiService _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly ILogger<SmsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<AppResponse<bool>> TwilioSendAsync(string phoneNumber, string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return AppResponses.Failure<bool>("Phone number is required.");
            }

            var formattedNumber = StandardizePhoneNumber(phoneNumber);
            var twilioConfig = _smsSettings.Twilio;

            // Twilio's MessageResource.CreateAsync returns a MessageResource object
            var result = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(twilioConfig.FromNumber),
                to: new PhoneNumber(formattedNumber)

            ).ConfigureAwait(false);

            // Check the status provided by Twilio
            return result.ErrorCode == null
                ? AppResponses.Success("SMS queued successfully", true)
                : AppResponses.Failure<bool>($"Failed to deliver SMS: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            HttpClientLogDefinitions.LogExternalApiError(_logger, "Twilio API Send", "N/A", ex);
            throw;
        }
    }

    private static string StandardizePhoneNumber(string number)
    {
        // Kenyan numbers:
        return !number.StartsWith('0')
            ? number.StartsWith("+254", StringComparison.Ordinal)
                ? number
                : throw new ArgumentException("Invalid phone number format. Expected 07... or +254...", nameof(number))
            : string.Concat("+254", number.AsSpan(1).ToString());
    }


}

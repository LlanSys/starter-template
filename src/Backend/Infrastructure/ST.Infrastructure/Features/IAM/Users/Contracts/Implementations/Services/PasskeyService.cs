using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts.Common;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.IAM.Users.Entities;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace ST.Infrastructure.Features.IAM.Users.Contracts.Implementations.Services;

public class PasskeyService : IPasskeyService
{
    private readonly IFido2 _fido2;
    private readonly PasskeyCredentialPolicy _credentialPolicy;

    public PasskeyService(IFido2 fido2, IIamUnitOfWork unitOfWork, ICurrentTenantProvider tenantProvider)
    {
        _fido2 = fido2;
        _credentialPolicy = new PasskeyCredentialPolicy(unitOfWork, tenantProvider);
    }

    public Task<JsonElement> RequestNewCredentialAsync(AppUser user, System.Collections.Generic.IEnumerable<Fido2Credential> existingCredentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        var fidoUser = new Fido2User
        {
            Name = user.UserName,
            Id = System.Text.Encoding.UTF8.GetBytes(user.Id),
            DisplayName = $"{user.FirstName} {user.LastName}"
        };

        var excludeCredentials = existingCredentials?.Select(c => new PublicKeyCredentialDescriptor(c.CredentialId)).ToList() ?? new System.Collections.Generic.List<PublicKeyCredentialDescriptor>();

        var options = _fido2.RequestNewCredential(
            new RequestNewCredentialParams
            {
                User = fidoUser,
                ExcludeCredentials = excludeCredentials,
                AuthenticatorSelection = new AuthenticatorSelection { UserVerification = UserVerificationRequirement.Required },
                AttestationPreference = AttestationConveyancePreference.None,
                Extensions = new AuthenticationExtensionsClientInputs()
            }
        );

        var jsonString = options.ToJson();
        using var document = JsonDocument.Parse(jsonString);
        return Task.FromResult(document.RootElement.Clone());
    }

    public async Task<Fido2Credential> MakeNewCredentialAsync(AppUser user, JsonElement attestationResponse, JsonElement originalOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user, nameof(user));
        if (!_credentialPolicy.CanUseAccount(user)) throw new ArgumentException("Account is unavailable.");
        var response = attestationResponse.Deserialize<AuthenticatorAttestationRawResponse>()
            ?? throw new ArgumentException("Invalid attestation response.");

        var options = originalOptions.Deserialize<CredentialCreateOptions>()
            ?? throw new ArgumentException("Invalid original options.");
        if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(options.User.Id, System.Text.Encoding.UTF8.GetBytes(user.Id)))
            throw new ArgumentException("Registration account does not match the challenge.");

        IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, token) =>
        {
            return await _credentialPolicy.IsUniqueAsync(user, args.CredentialId, token).ConfigureAwait(false);
        };

        var success = await _fido2.MakeNewCredentialAsync(
            new MakeNewCredentialParams
            {
                AttestationResponse = response,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = callback
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var credential = new Fido2Credential
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            CredentialId = success.Id,
            PublicKey = success.PublicKey,
            UserHandle = System.Text.Encoding.UTF8.GetBytes(user.Id),
            SignatureCounter = success.SignCount,
            CredType = success.Type.ToString(),
            RegDate = DateTimeOffset.UtcNow,
            AaGuid = success.AaGuid,
            CreatedBy = user.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return credential;
    }

    public Task<JsonElement> RequestAssertionAsync(string username, CancellationToken cancellationToken = default)
    {
        var options = _fido2.GetAssertionOptions(
            new GetAssertionOptionsParams
            {
                AllowedCredentials = [],
                UserVerification = UserVerificationRequirement.Required
            }
        );

        var jsonString = options.ToJson();
        using var document = JsonDocument.Parse(jsonString);
        return Task.FromResult(document.RootElement.Clone());
    }

    public async Task<uint?> MakeAssertionAsync(AppUser user, JsonElement assertionResponse, JsonElement originalOptions, byte[] storedPublicKey, uint storedSignCount, CancellationToken cancellationToken = default)
    {
        if (!_credentialPolicy.CanUseAccount(user)) throw new ArgumentException("Account is unavailable.");
        var response = assertionResponse.Deserialize<AuthenticatorAssertionRawResponse>()
            ?? throw new ArgumentException("Invalid assertion response.");

        var options = originalOptions.Deserialize<AssertionOptions>()
            ?? throw new ArgumentException("Invalid assertion options.");

        IsUserHandleOwnerOfCredentialIdAsync callback = async (args, token) =>
        {
            return await _credentialPolicy.IsOwnerAsync(user, args.CredentialId, args.UserHandle, token).ConfigureAwait(false);
        };

        var res = await _fido2.MakeAssertionAsync(
            new MakeAssertionParams
            {
                AssertionResponse = response,
                OriginalOptions = options,
                StoredPublicKey = storedPublicKey,
                StoredSignatureCounter = storedSignCount,
                IsUserHandleOwnerOfCredentialIdCallback = callback
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return res.SignCount;
    }
}

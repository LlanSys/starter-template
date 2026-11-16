using FluentValidation.TestHelper;
using ST.Application.Features.ControlPlane.Kyc.Commands;
using ST.Application.Features.ControlPlane.Kyc.Validators;
using ST.Infrastructure.Configuration;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;

namespace ST.Tests.Unit.ControlPlane.Tenants;

public sealed class KycDocumentTypeTests
{
    [Theory]
    [InlineData("other", null, false)]
    [InlineData("other", "Trading permit", true)]
    [InlineData("business-registration", null, true)]
    [InlineData("business-registration", "Injected label", false)]
    public void UploadRequiresOtherNameOnlyForOther(string key, string? name, bool valid)
    {
        using var stream = new MemoryStream();
        var request = new UploadKycDocumentCommand(Guid.CreateVersion7(), null, key, "test.pdf", 20, stream, name);
        Assert.Equal(valid, new UploadKycDocumentCommandValidator().TestValidate(request).IsValid);
    }

    [Theory]
    [InlineData("business-registration", true)]
    [InlineData("other", false)]
    [InlineData("legacy", false)]
    [InlineData("Invalid key", false)]
    public void CatalogueRejectsReservedOrUnstableKeys(string key, bool valid)
    {
        var settings = new KycDocumentStorageSettings
        {
            DocumentTypes = [new KycDocumentTypeOption(key, "Registration", "Requested evidence.")]
        };
        Assert.Equal(valid, settings.HasValidDocumentTypes());
    }

    [Fact]
    public void CatalogueRejectsDuplicateKeys()
    {
        var settings = new KycDocumentStorageSettings
        {
            DocumentTypes = [new("registration", "Registration", ""), new("registration", "Duplicate", "")]
        };
        Assert.False(settings.HasValidDocumentTypes());
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using ST.Infrastructure.Features.ControlPlane.Kyc;
using Microsoft.Extensions.Hosting;
using ST.SharedKernel.Features.ControlPlane;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Infrastructure.Features.ControlPlane.Kyc.Storage;
using Microsoft.Extensions.Options;
using ST.Domain.Shared.Contracts.Common;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ST.Application.Features.ControlPlane.Tenants.Contracts;
using ST.Application.Features.IAM.Users.Contracts.Interfaces;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Features.IAM.Users.Entities;
using ST.Infrastructure.Configuration;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed class KycEvidenceTests(StarterTemplateWebApplicationFactory factory, KycScannerFixture scanner) : IClassFixture<KycScannerFixture>
{
    [Fact]
    public async Task PrivateEvidenceSupportsPlatformAndClientUploadsWithRevocableWorkspaceScopedLinks()
    {
        var accountKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await using var azurite = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:3.35.0")
            .WithEnvironment("AZURITE_ACCOUNTS", $"kyctest:{accountKey}")
            .WithPortBinding(10000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(10000))
            .Build();

        await azurite.StartAsync();
        var connection = $"DefaultEndpointsProtocol=http;AccountName=kyctest;AccountKey={accountKey};BlobEndpoint=http://{azurite.Hostname}:{azurite.GetMappedPublicPort(10000)}/kyctest;";
        var scannerUnavailable = false;
        using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // This synthetic matrix deliberately exceeds the normal 20 uploads/hour budget.
                ["RateLimiting:FileUploadPolicy:PermitLimit"] = "100",
                ["RateLimiting:FileUploadPolicy:QueueLimit"] = "0",
                ["KycDocumentSafety:Host"] = scanner.Host,
                ["KycDocumentSafety:Port"] = scanner.Port.ToString(),
                ["KycDocumentSafety:TimeoutSeconds"] = "120",
                ["KycDocuments:Provider"] = "Azurite",
                ["KycDocuments:ConnectionString"] = connection,
                ["KycDocuments:ContainerName"] = "kyc-certification",
                ["KycDocuments:DocumentTypes:5:Key"] = "sector-license",
                ["KycDocuments:DocumentTypes:5:Name"] = "Sector license",
                ["KycDocuments:DocumentTypes:5:Guidance"] = "Synthetic configurable type."
            })).ConfigureTestServices(services =>
            {
                foreach (var hosted in services.Where(item => item.ServiceType == typeof(IHostedService)
                    && item.ImplementationType == typeof(KycDocumentSafetyWorker)).ToArray())
                {
                    services.Remove(hosted);
                }
                services.AddScoped<IKycDocumentScanner>(provider => new FailureSwitchScanner(
                    new ClamAvKycDocumentScanner(provider.GetRequiredService<IOptions<KycDocumentSafetySettings>>(),
                        provider.GetRequiredService<IOptions<KycDocumentStorageSettings>>(), TimeProvider.System,
                        NullLogger<ClamAvKycDocumentScanner>.Instance), () => scannerUnavailable));
            }));

        var tenant = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();

        await using (var scope = configured.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();

            foreach (var id in new[] { tenant, other })
            {
                db.Tenants.Add(new Tenant
                {
                    Id = id,
                    Identifier = $"kyc-{id:N}",
                    DisplayName = "KYC certification",
                    HostName = $"{id:N}.invalid",
                    CreatedBy = "fixture",
                    DeploymentStampId = Tenant.PlatformTenantId,
                    MaxUsers = 10
                });
            }

            await db.SaveChangesAsync();
        }

        using var vendor = await VendorAsync(configured);
        using var anonymous = configured.CreateClient(new()
        {
            HandleCookies = false
        });

        var root = $"/api/v1/control-plane/tenants/{tenant}/kyc";
        var publicRoot = $"/api/v1/onboarding/kyc/{tenant}";
        var link = await PostAsync<KycSubmissionLinkResponse>(vendor, $"{root}/submission-link", null);
        Assert.Equal(64, link.Token.Length);
        var preview = await PostAsync<KycSubmissionStatusResponse>(anonymous, $"{publicRoot}/inspect", new InspectKycSubmissionRequest(link.Token));
        Assert.Equal("KYC certification", preview.WorkspaceName);
        Assert.Equal(20, preview.RemainingUploads);
        Assert.Contains(preview.DocumentTypes, type => type.Key == "business-registration");
        Assert.Contains(preview.DocumentTypes, type => type.Key == "sector-license");
        var caseInfo = (await vendor.GetFromJsonAsync<AppResponse<KycCaseSummaryResponse>>($"{root}/case"))!.Data!;
        Assert.Equal(preview.DocumentTypes, caseInfo.DocumentTypes);

        var staffBytes = Encoding.UTF8.GetBytes("%PDF-1.4\nstaff evidence\n%%EOF");
        var staffDocument = await UploadAsync(vendor, $"{root}/documents", staffBytes, "registration.pdf", null);
        var clientBytes = Encoding.UTF8.GetBytes("%PDF-1.4\nclient evidence\n%%EOF");
        var clientDocument = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "authority.pdf", link.Token);
        var duplicate = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "authority.pdf", link.Token);
        Assert.Equal(clientDocument, duplicate);
        preview = await PostAsync<KycSubmissionStatusResponse>(anonymous, $"{publicRoot}/inspect", new InspectKycSubmissionRequest(link.Token));
        Assert.Single(preview.Documents);
        Assert.Equal(19, preview.RemainingUploads);
        Assert.Equal("authority.pdf", preview.Documents[0].FileName);

        using (var forbidden = await anonymous.GetAsync($"{root}/documents/{clientDocument}/content"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);
        }

        using (var foreign = await vendor.GetAsync($"/api/v1/control-plane/tenants/{other}/kyc/documents/{clientDocument}/content"))
        {
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        }

        using (var pending = await vendor.GetAsync($"{root}/documents/{clientDocument}/content"))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, pending.StatusCode);
        }

        using (var pendingReview = await vendor.PostAsJsonAsync($"{root}/documents/{clientDocument}/reviews", new ReviewKycDocumentRequest(true, "Premature acceptance.")))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, pendingReview.StatusCode);
        }

        await configured.Services.GetRequiredService<IKycDocumentSafetyProcessor>().ProcessBatchAsync(default);

        await using (var scanScope = configured.Services.CreateAsyncScope())
        {
            var state = await scanScope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>().KycDocuments.SingleAsync(item => item.Id == clientDocument);
            Assert.True(state.SafetyStatus.ToString() == "Clean", $"Safety result: {state.SafetyStatus}; reason: {state.SafetyReasonCode}; definitions: {state.SafetyDefinitionsAt}");
        }

        using (var download = await vendor.GetAsync($"{root}/documents/{clientDocument}/content"))
        {
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal(clientBytes, await download.Content.ReadAsByteArrayAsync());
            Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
            Assert.True(download.Headers.CacheControl!.NoStore);
        }

        await PostAsync<bool>(vendor, $"{root}/documents/{clientDocument}/reviews", new ReviewKycDocumentRequest(true, "Authority evidence verified."));
        await PostAsync<bool>(vendor, $"{root}/documents/{clientDocument}/reviews", new ReviewKycDocumentRequest(false, "Please supply a clearer scan."));
        var list = await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents");
        Assert.Equal(2, list!.Data!.Count);
        Assert.Equal(2, list.Data.Single(item => item.Id == clientDocument).Reviews.Count);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(clientBytes)), list.Data.Single(item => item.Id == clientDocument).Sha256);
        preview = await PostAsync<KycSubmissionStatusResponse>(anonymous, $"{publicRoot}/inspect", new InspectKycSubmissionRequest(link.Token));
        Assert.Equal("Please supply a clearer scan.", preview.Documents[0].Feedback);

        var blobs = new BlobContainerClient(connection, "kyc-certification",
            new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03));

        var unsafeBytes = Convert.FromBase64String("JVBERi0xLjMKJeLjz9MKMSAwIG9iago8PAovUHJvZHVjZXIgKHB5cGRmKQo+PgplbmRvYmoKMiAwIG9iago8PAovVHlwZSAvUGFnZXMKL0NvdW50IDEKL0tpZHMgWyA0IDAgUiBdCj4+CmVuZG9iagozIDAgb2JqCjw8Ci9UeXBlIC9DYXRhbG9nCi9QYWdlcyAyIDAgUgovTmFtZXMgNyAwIFIKPj4KZW5kb2JqCjQgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1Jlc291cmNlcyA8PAo+PgovTWVkaWFCb3ggWyAwLjAgMC4wIDEwMCAxMDAgXQovUGFyZW50IDIgMCBSCj4+CmVuZG9iago1IDAgb2JqCjw8Ci9UeXBlIC9FbWJlZGRlZEZpbGUKL0xlbmd0aCA2OAo+PgpzdHJlYW0KWDVPIVAlQEFQWzRcUFpYNTQoUF4pN0NDKTd9JEVJQ0FSLVNUQU5EQVJELUFOVElWSVJVUy1URVNULUZJTEUhJEgrSCoKZW5kc3RyZWFtCmVuZG9iago2IDAgb2JqCjw8Ci9UeXBlIC9GaWxlc3BlYwovRiAoc3ludGhldGljXDA1NWFudGl2aXJ1c1wwNTV0ZXN0XDA1NnR4dCkKL0VGIDw8Ci9GIDUgMCBSCj4+Cj4+CmVuZG9iago3IDAgb2JqCjw8Ci9FbWJlZGRlZEZpbGVzIDggMCBSCj4+CmVuZG9iago4IDAgb2JqCjw8Ci9OYW1lcyBbIChzeW50aGV0aWNcMDU1YW50aXZpcnVzXDA1NXRlc3RcMDU2dHh0KSA2IDAgUiBdCj4+CmVuZG9iagp4cmVmCjAgOQowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDA1NCAwMDAwMCBuIAowMDAwMDAwMTEzIDAwMDAwIG4gCjAwMDAwMDAxNzUgMDAwMDAgbiAKMDAwMDAwMDI2OSAwMDAwMCBuIAowMDAwMDAwNDA3IDAwMDAwIG4gCjAwMDAwMDA1MDYgMDAwMDAgbiAKMDAwMDAwMDU0OCAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDkKL1Jvb3QgMyAwIFIKL0luZm8gMSAwIFIKPj4Kc3RhcnR4cmVmCjYyNgolJUVPRgo=");
        var unsafeDocument = await UploadAsync(vendor, $"{root}/documents", unsafeBytes, "synthetic-unsafe.pdf", null);
        var encryptedBytes = Convert.FromBase64String("JVBERi0xLjMKJeLjz9MKMSAwIG9iago8PAovUHJvZHVjZXIgPDBhOTM3ZWZhZTQ+Cj4+CmVuZG9iagoyIDAgb2JqCjw8Ci9UeXBlIC9QYWdlcwovQ291bnQgMQovS2lkcyBbIDQgMCBSIF0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL0NhdGFsb2cKL1BhZ2VzIDIgMCBSCj4+CmVuZG9iago0IDAgb2JqCjw8Ci9UeXBlIC9QYWdlCi9SZXNvdXJjZXMgPDwKPj4KL01lZGlhQm94IFsgMC4wIDAuMCAxMDAgMTAwIF0KL1BhcmVudCAyIDAgUgo+PgplbmRvYmoKNSAwIG9iago8PAovViAyCi9SIDMKL0xlbmd0aCAxMjgKL1AgNDI5NDk2NzI5MgovRmlsdGVyIC9TdGFuZGFyZAovTyA8YWJhNjE5NGY5ODI5ZWRhOTM3MTk0NDJjMjE5NTUwMTU3ZWQwNTBjMDRhZjRiYWMyZTEwOTkyMzY2MDcyZGU0MD4KL1UgPGEwNWUyNGE5NGVjYzI2MTc5ZDc0MWVjODMyYjE2MDNkMjhiZjRlNWU0ZTc1OGE0MTY0MDA0ZTU2ZmZmYTAxMDg+Cj4+CmVuZG9iagp4cmVmCjAgNgowMDAwMDAwMDAwIDY1NTM1IGYgCjAwMDAwMDAwMTUgMDAwMDAgbiAKMDAwMDAwMDA1OSAwMDAwMCBuIAowMDAwMDAwMTE4IDAwMDAwIG4gCjAwMDAwMDAxNjcgMDAwMDAgbiAKMDAwMDAwMDI2MSAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9TaXplIDYKL1Jvb3QgMyAwIFIKL0luZm8gMSAwIFIKL0lEIFsgPDJhYjAyN2EyYzUxOWNiOTI5OGM2OTRlMjBkZTc4MzU0PiA8MmFiMDI3YTJjNTE5Y2I5Mjk4YzY5NGUyMGRlNzgzNTQ+IF0KL0VuY3J5cHQgNSAwIFIKPj4Kc3RhcnR4cmVmCjQ3NgolJUVPRgo=");
        var encryptedDocument = await UploadAsync(vendor, $"{root}/documents", encryptedBytes, "synthetic-encrypted.pdf", null);
        await configured.Services.GetRequiredService<IKycDocumentSafetyProcessor>().ProcessBatchAsync(default);
        var safetyList = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents"))!.Data!;
        Assert.Equal("Unsafe", safetyList.Single(item => item.Id == unsafeDocument).SafetyStatus);
        Assert.Equal("Blocked", safetyList.Single(item => item.Id == encryptedDocument).SafetyStatus);

        foreach (var blockedId in new[] { unsafeDocument, encryptedDocument })
        {
            using var blocked = await vendor.GetAsync($"{root}/documents/{blockedId}/content");
            Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);
            using var rejected = await vendor.PostAsJsonAsync($"{root}/documents/{blockedId}/reviews", new ReviewKycDocumentRequest(true, "Cannot accept unsafe evidence."));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        }
        var unsafeItem = safetyList.Single(item => item.Id == unsafeDocument);
        await PostAsync<bool>(vendor, $"{root}/documents/{unsafeDocument}/safety-retry", new RetryKycSafetyScanRequest(unsafeItem.Revision));

        using (var stale = await vendor.PostAsJsonAsync($"{root}/documents/{unsafeDocument}/safety-retry", new RetryKycSafetyScanRequest(unsafeItem.Revision)))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, stale.StatusCode);
        }

        using (var prematureApproval = await vendor.PostAsJsonAsync($"/api/v1/control-plane/tenants/{tenant}/approve", new ApproveTenantRequest("Uncleared evidence must block approval")))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, prematureApproval.StatusCode);
        }
        scannerUnavailable = true;
        var outageDocument = await UploadAsync(vendor, $"{root}/documents", Encoding.UTF8.GetBytes("%PDF-1.4\noutage evidence\n%%EOF"), "outage.pdf", null);
        await configured.Services.GetRequiredService<IKycDocumentSafetyProcessor>().ProcessBatchAsync(default);
        var outageList = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents"))!.Data!;
        var outageItem = outageList.Single(item => item.Id == outageDocument);
        Assert.Equal("Failed", outageItem.SafetyStatus);

        using (var unavailable = await vendor.GetAsync($"{root}/documents/{outageDocument}/content"))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, unavailable.StatusCode);
        }

        scannerUnavailable = false;
        await PostAsync<bool>(vendor, $"{root}/documents/{outageDocument}/safety-retry", new RetryKycSafetyScanRequest(outageItem.Revision));
        await configured.Services.GetRequiredService<IKycDocumentSafetyProcessor>().ProcessBatchAsync(default);
        var recoveredList = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents"))!.Data!;
        Assert.Equal("Clean", recoveredList.Single(item => item.Id == outageDocument).SafetyStatus);
        // Even a previously cleared blob cannot be served after its original bytes change.
        await blobs.GetBlobClient($"kyc/{tenant:N}/{staffDocument:N}").UploadAsync(new BinaryData(clientBytes), overwrite: true);

        using (var changed = await vendor.GetAsync($"{root}/documents/{staffDocument}/content"))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, changed.StatusCode);
        }

        using (var direct = new HttpClient())
        using (var response = await direct.GetAsync(blobs.GetBlobClient($"kyc/{tenant:N}/{staffDocument:N}").Uri))
        {
            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        }

        await using (var scope = configured.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            var storedLink = await db.KycSubmissionLinks.SingleAsync(item => item.TenantId == tenant);
            Assert.NotEqual(link.Token, storedLink.TokenHash);
            Assert.Equal(64, storedLink.TokenHash.Length);
        }

        using (var customer = await VendorAsync(configured, platformStaff: false))
        using (var denied = await customer.GetAsync($"{root}/documents"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
        var rotated = await PostAsync<KycSubmissionLinkResponse>(vendor, $"{root}/submission-link", null);
        var resubmitted = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "authority.pdf", rotated.Token);
        Assert.NotEqual(clientDocument, resubmitted);
        var rotatedPreview = await PostAsync<KycSubmissionStatusResponse>(anonymous, $"{publicRoot}/inspect", new InspectKycSubmissionRequest(rotated.Token));
        Assert.Single(rotatedPreview.Documents);
        Assert.Equal(19, rotatedPreview.RemainingUploads);

        using (var old = await anonymous.PostAsJsonAsync($"{publicRoot}/inspect", new InspectKycSubmissionRequest(link.Token)))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, old.StatusCode);
        }

        using (var wrongWorkspace = await anonymous.PostAsJsonAsync($"/api/v1/onboarding/kyc/{other}/inspect", new InspectKycSubmissionRequest(rotated.Token)))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongWorkspace.StatusCode);
        }

        using (var revoked = await vendor.DeleteAsync($"{root}/submission-link"))
        {
            revoked.EnsureSuccessStatusCode();
        }

        using (var closed = await anonymous.PostAsJsonAsync($"{publicRoot}/inspect", new InspectKycSubmissionRequest(rotated.Token)))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, closed.StatusCode);
        }

        var finalLink = await PostAsync<KycSubmissionLinkResponse>(vendor, $"{root}/submission-link", null);

        using (var invalidForm = Form(Encoding.UTF8.GetBytes("not a PDF"), "invalid.pdf", finalLink.Token))
        using (var invalidUpload = await anonymous.PostAsync($"{publicRoot}/documents", invalidForm))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidUpload.StatusCode);
        }

        using (var oversizedForm = Form(new byte[10_485_761], "oversized.pdf", finalLink.Token))
        using (var oversized = await anonymous.PostAsync($"{publicRoot}/documents", oversizedForm))
        {
            Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
        }

        using (var wrongExtensionForm = Form(clientBytes, "disguised.png", finalLink.Token))
        using (var wrongExtension = await anonymous.PostAsync($"{publicRoot}/documents", wrongExtensionForm))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongExtension.StatusCode);
        }

        using (var missingTokenForm = Form(clientBytes, "missing.pdf", null))
        using (var missingToken = await anonymous.PostAsync($"{publicRoot}/documents", missingTokenForm))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, missingToken.StatusCode);
        }

        foreach (var invalidType in new[] { "unknown-type", "legacy", "other" })
        {
            using var invalidTypeForm = Form(clientBytes, "invalid-type.pdf", finalLink.Token, invalidType);
            using var invalidTypeResponse = await anonymous.PostAsync($"{publicRoot}/documents", invalidTypeForm);
            Assert.Equal(invalidType == "other" ? HttpStatusCode.BadRequest : HttpStatusCode.UnprocessableEntity, invalidTypeResponse.StatusCode);
        }

        using (var invalidNameForm = Form(clientBytes, "invalid-name.pdf", finalLink.Token, "business-registration", "Unlisted name"))
        using (var invalidNameResponse = await anonymous.PostAsync($"{publicRoot}/documents", invalidNameForm))
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalidNameResponse.StatusCode);
        }
        var typed = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "tax.pdf", finalLink.Token, "tax-registration");
        var custom = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "permit.pdf", finalLink.Token, "other", "Trading permit");
        var retryCustom = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "permit.pdf", finalLink.Token, "other", "Trading permit");
        var secondCustom = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "license.pdf", finalLink.Token, "other", "Professional license");
        Assert.Equal(custom, retryCustom);
        Assert.NotEqual(typed, custom);
        Assert.NotEqual(custom, secondCustom);
        var configuredType = await UploadAsync(anonymous, $"{publicRoot}/documents", clientBytes, "sector.pdf", finalLink.Token, "sector-license");
        var platformCustom = await UploadAsync(vendor, $"{root}/documents", staffBytes, "staff-permit.pdf", null, "other", "Trading permit");
        var classified = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents"))!.Data!;
        Assert.Equal("business-registration", classified.Single(item => item.Id == staffDocument).DocumentTypeKey);
        Assert.Equal("Business registration", classified.Single(item => item.Id == staffDocument).DocumentType);
        Assert.Equal("tax-registration", classified.Single(item => item.Id == typed).DocumentTypeKey);
        Assert.Equal("Trading permit", classified.Single(item => item.Id == platformCustom).DocumentType);
        Assert.Equal("other", classified.Single(item => item.Id == custom).DocumentTypeKey);
        var typedPreview = await PostAsync<KycSubmissionStatusResponse>(anonymous, $"{publicRoot}/inspect", new InspectKycSubmissionRequest(finalLink.Token));
        Assert.Equal(4, typedPreview.Documents.Count);
        Assert.Equal(16, typedPreview.RemainingUploads);
        Assert.Contains(typedPreview.Documents, item => item.DocumentTypeKey == "other" && item.DocumentType == "Trading permit");
        var filtered = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents?documentTypeKey=sector-license"))!.Data!;
        Assert.Equal(configuredType, Assert.Single(filtered).Id);

        await using (var classificationScope = configured.Services.CreateAsyncScope())
        {
            var db = classificationScope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            Assert.Equal(typed, (await db.KycDocuments.SingleAsync(item => item.TenantId == tenant
                && item.DocumentTypeKey == "tax-registration")).Id);
        }

        await configured.Services.GetRequiredService<IKycDocumentSafetyProcessor>().ProcessBatchAsync(default);
        var decisionDocuments = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycDocumentResponse>>>($"{root}/documents"))!.Data!;

        foreach (var blocked in decisionDocuments.Where(item => item.SafetyStatus != "Clean"))
        {
            await PostAsync<bool>(vendor, $"{root}/documents/{blocked.Id}/reviews", new ReviewKycDocumentRequest(false, "Replace evidence that could not pass safety checks."));
        }

        using (var approved = await vendor.PostAsJsonAsync($"/api/v1/control-plane/tenants/{tenant}/approve", new
        {
            EvidenceReference = "KYC certification case"
        }))
        {
            approved.EnsureSuccessStatusCode();
        }

        using (var closed = await anonymous.PostAsJsonAsync($"{publicRoot}/inspect", new InspectKycSubmissionRequest(finalLink.Token)))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, closed.StatusCode);
        }
        var otherRoot = $"/api/v1/control-plane/tenants/{other}/kyc";
        var otherPublic = $"/api/v1/onboarding/kyc/{other}";
        var racingLink = await PostAsync<KycSubmissionLinkResponse>(vendor, $"{otherRoot}/submission-link", null);
        var gate = new BlockingStorage(new BlobKycDocumentStorage(Options.Create(new KycDocumentStorageSettings
        {
            Provider = "Azurite",
            ConnectionString = connection,
            ContainerName = "kyc-certification"
        })));

        using (var racingHost = configured.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IKycDocumentStorage>(gate))))
        using (var racingClient = racingHost.CreateClient())
        using (var racingForm = Form(Encoding.UTF8.GetBytes("%PDF-1.4\nracing document\n%%EOF"), "race.pdf", racingLink.Token))
        {
            var upload = racingClient.PostAsync($"{otherPublic}/documents", racingForm);
            await gate.Stored.Task.WaitAsync(TimeSpan.FromSeconds(30));
            using var revoked = await vendor.DeleteAsync($"{otherRoot}/submission-link");
            revoked.EnsureSuccessStatusCode();
            gate.Resume.TrySetResult();
            using var result = await upload;
            Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
            Assert.False((await blobs.GetBlobClient(gate.Key!).ExistsAsync()).Value);
        }

        var expiredLink = await PostAsync<KycSubmissionLinkResponse>(vendor, $"{otherRoot}/submission-link", null);

        await using (var scope = configured.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            await db.KycSubmissionLinks.Where(item => item.TenantId == other)
                .ExecuteUpdateAsync(update => update.SetProperty(item => item.ExpiresAt, DateTimeOffset.UnixEpoch));
        }

        using (var expired = await anonymous.PostAsJsonAsync($"{otherPublic}/inspect", new InspectKycSubmissionRequest(expiredLink.Token)))
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, expired.StatusCode);
        }
    }

    private sealed class BlockingStorage(IKycDocumentStorage inner) : IKycDocumentStorage
    {
        public TaskCompletionSource Stored { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? Key
        {
            get; private set;
        }

        public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            await inner.SaveAsync(key, content, contentType, cancellationToken);
            Key = key;
            Stored.TrySetResult();
            await Resume.Task.WaitAsync(cancellationToken);
        }

        public Task<Stream?> OpenAsync(string key, CancellationToken cancellationToken)
        {
            return inner.OpenAsync(key, cancellationToken);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            return inner.DeleteAsync(key, cancellationToken);
        }
    }

    internal static async Task<HttpClient> VendorAsync(WebApplicationFactory<Program> app, bool platformStaff = true)
    {
        await using var scope = app.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IWorkspaceTenantOverride>().TenantId = Tenant.PlatformTenantId;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var name = $"kyc-reviewer-{Guid.CreateVersion7():N}";
        var user = AppUser.CreateSystemUser(DateTimeOffset.UnixEpoch, Tenant.PlatformTenantId, name, $"{name}@example.test", "fixture");
        user.IsActive = true;
        user.EmailConfirmed = true;
        user.TwoFactorEnabled = true;
        Assert.True((await users.CreateAsync(user)).Succeeded);

        if (platformStaff)
        {
            Assert.True((await users.AddClaimsAsync(user,
            PlatformPermissions.All.Select(key => new Claim("permission", key)).Append(new Claim("platform_staff", "true")))).Succeeded);
        }
        var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>();
        var session = await sessions.CreateSessionAsync(user.Id, Guid.CreateVersion7(), "127.0.0.1", "certification");
        Assert.True(session.IsSuccess);
        var claims = await scope.ServiceProvider.GetRequiredService<IClaimsService>().GetUserClaimsAsync(user, session.Data);
        claims.Add(new Claim("amr", "mfa"));
        var token = await scope.ServiceProvider.GetRequiredService<IJwtService>().CreateTokenAsync(claims);
        var client = app.CreateClient(new()
        {
            HandleCookies = false
        });

        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private static async Task<T> PostAsync<T>(HttpClient client, string path, object? request)
    {
        using var response = request is null ? await client.PostAsync(path, null) : await client.PostAsJsonAsync(path, request);
        Assert.True(response.IsSuccessStatusCode, $"KYC request returned HTTP {(int)response.StatusCode}.");
        var result = await response.Content.ReadFromJsonAsync<AppResponse<T>>();
        Assert.True(result!.IsSuccess);
        return result.Data!;
    }

    private static async Task<Guid> UploadAsync(HttpClient client, string path, byte[] bytes, string name, string? token, string type = "business-registration", string? otherName = null)
    {
        using var form = Form(bytes, name, token, type, otherName);
        using var response = await client.PostAsync(path, form);
        Assert.True(response.IsSuccessStatusCode, $"KYC upload returned HTTP {(int)response.StatusCode}.");
        var result = await response.Content.ReadFromJsonAsync<AppResponse<Guid>>();
        return result!.Data;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The returned MultipartFormDataContent owns and disposes all its added content parts.")]
    private static MultipartFormDataContent Form(byte[] bytes, string name, string? token, string type = "business-registration", string? otherName = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "File", name);
        form.Add(new StringContent(type), "DocumentType");

        if (otherName is not null)
        {
            form.Add(new StringContent(otherName), "OtherDocumentName");
        }

        if (token is not null)
        {
            form.Add(new StringContent(token), "Token");
        }
        return form;
    }
    private sealed class FailureSwitchScanner(IKycDocumentScanner inner, Func<bool> unavailable) : IKycDocumentScanner
    {
        public Task<KycScanResult> ScanAsync(Stream content, CancellationToken cancellationToken)
        {
            return unavailable()
                ? Task.FromResult(new KycScanResult(KycScanVerdict.Unavailable, "Test dependency outage", "", null, "scanner-unavailable"))
                : inner.ScanAsync(content, cancellationToken);
        }
    }
}

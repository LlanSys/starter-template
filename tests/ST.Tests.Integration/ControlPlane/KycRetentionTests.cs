using Azure;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Security.Cryptography;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using FluentValidation;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ST.Application.Features.ControlPlane.Kyc.Contracts;
using ST.Domain.Features.ControlPlane.Contracts;
using ST.Domain.Features.ControlPlane.Kyc.Entities;
using ST.Domain.Features.ControlPlane.Tenants.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Infrastructure.Features.ControlPlane.Kyc;
using ST.Infrastructure.Features.ControlPlane.Kyc.Storage;
using ST.Persistence.Features.ControlPlane.DataContext;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
using ST.Tests.Integration.TestFixtures;

namespace ST.Tests.Integration.ControlPlane;

[Collection("Starter API")]
public sealed class KycRetentionTests(StarterTemplateWebApplicationFactory factory)
{
    [Fact]
    public async Task VersionedPolicyHoldsRecoveryRetriesErasureAndOrphanReconciliationWorkAgainstPrivateAzurite()
    {
        var accountKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        await using var azurite = new ContainerBuilder().WithImage("mcr.microsoft.com/azure-storage/azurite:3.35.0")
            .WithEnvironment("AZURITE_ACCOUNTS", $"retentiontest:{accountKey}").WithPortBinding(10000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(10000)).Build();

        await azurite.StartAsync();
        var connection = $"DefaultEndpointsProtocol=http;AccountName=retentiontest;AccountKey={accountKey};BlobEndpoint=http://{azurite.Hostname}:{azurite.GetMappedPublicPort(10000)}/retentiontest;";
        var time = new RetentionClock(TimeProvider.System.GetUtcNow());
        var failure = new ProviderResult();
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KycDocuments:Provider"] = "Azurite",
                ["KycDocuments:ConnectionString"] = connection,
                ["KycDocuments:ContainerName"] = "kyc-retention-tests",
                ["KycDocuments:ErasureLedgerContainerName"] = "retention-ledger-tests"
            }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IKycEvidenceLifecycleStorage>(sp => new ControlledStorage(sp.GetRequiredService<BlobKycDocumentStorage>(), failure));
                services.AddScoped<IKycRetentionService>(sp => new KycRetentionService(
                    sp.GetRequiredService<IControlPlaneUnitOfWork>(), sp.GetRequiredService<IKycEvidenceLifecycleStorage>(),
                    sp.GetRequiredService<ICurrentActorProvider>(), time,
                    sp.GetRequiredService<IValidator<PublishKycRetentionPolicyRequest>>(), sp.GetRequiredService<IValidator<KycRetentionActionRequest>>(),
                    sp.GetRequiredService<ILogger<KycRetentionService>>(), sp.GetRequiredService<KycOrphanScanProgress>()));
            });
        });

        var tenantId = Guid.CreateVersion7();

        await using (var scope = app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Identifier = $"retention-{tenantId:N}",
                DisplayName = "Synthetic retention case",
                HostName = $"{tenantId:N}.invalid",
                DeploymentStampId = Tenant.PlatformTenantId,
                CreatedBy = "fixture",
                MaxUsers = 5
            });

            await db.SaveChangesAsync();
        }
        using var vendor = await KycEvidenceTests.VendorAsync(app);
        using var noGrant = await KycEvidenceTests.VendorAsync(app, false);
        const string root = "/api/v1/control-plane/kyc/retention";

        using (var denied = await noGrant.GetAsync($"{root}/policies"))
        {
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        var originalPolicy = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycRetentionPolicyResponse>>>($"{root}/policies"))!.Data![0];
        Assert.Equal(24, originalPolicy.ApprovedMonths);
        Assert.Equal(90, originalPolicy.ClosedDays);
        var initial = (await vendor.GetFromJsonAsync<AppResponse<KycRetentionStatusResponse>>($"{root}/cases/{tenantId}"))!.Data!;
        Guid published;

        try
        {
            var request = new PublishKycRetentionPolicyRequest(originalPolicy.Id, "Synthetic short schedule", 1, 1, 1, 1, 1, 1, "Synthetic acceptance policy");

            using (var denied = await noGrant.PostAsJsonAsync($"{root}/policies", request))
            {
                Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            }

            using var publish = await vendor.PostAsJsonAsync($"{root}/policies", request);
            publish.EnsureSuccessStatusCode();
            published = (await publish.Content.ReadFromJsonAsync<AppResponse<Guid>>())!.Data;

            using (var stale = await vendor.PostAsJsonAsync($"{root}/policies", request))
            {
                Assert.Equal(HttpStatusCode.UnprocessableEntity, stale.StatusCode);
            }

            var pinned = (await vendor.GetFromJsonAsync<AppResponse<KycRetentionStatusResponse>>($"{root}/cases/{tenantId}"))!.Data!;
            Assert.Equal(originalPolicy.Id, pinned.PolicyId);
            using var apply = await vendor.PostAsJsonAsync($"{root}/cases/{tenantId}", new KycRetentionActionRequest(pinned.Revision, "apply-current-policy", "Explicitly apply synthetic schedule"));
            apply.EnsureSuccessStatusCode();

            using (var wrongCapability = await vendor.PostAsJsonAsync($"{root}/cases/{tenantId}", new KycRetentionActionRequest(pinned.Revision, "hold", "Wrong endpoint")))
            {
                Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongCapability.StatusCode);
            }

            var bytes = Encoding.UTF8.GetBytes("%PDF-1.4\nSynthetic retention evidence\n%%EOF");
            Guid documentId;

            using (var form = new MultipartFormDataContent())
            {
                using var typeContent = new StringContent("business-registration");
                form.Add(typeContent, "DocumentType");
                using var fileContent = new ByteArrayContent(bytes);
                form.Add(fileContent, "File", "synthetic.pdf");
                using var upload = await vendor.PostAsync($"/api/v1/control-plane/tenants/{tenantId}/kyc/documents", form);
                upload.EnsureSuccessStatusCode();
                documentId = (await upload.Content.ReadFromJsonAsync<AppResponse<Guid>>())!.Data;
            }
            await ActAsync("Declined");
            await ActAsync("hold");
            time.Advance(TimeSpan.FromDays(3));
            await ProcessAsync();
            Assert.Equal("Retained", (await StatusAsync()).Phase);
            Assert.True((await StatusAsync()).Held);
            await ActAsync("release");
            await ProcessAsync();
            Assert.Equal("Recovery", (await StatusAsync()).Phase);

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var download = await scope.ServiceProvider.GetRequiredService<IKycEvidenceService>().DownloadAsync(tenantId, documentId, default);
                Assert.False(download.IsSuccess);
            }
            await ActAsync("restore");
            Assert.Equal("Open", (await StatusAsync()).Outcome);
            await ActAsync("Withdrawn");
            time.Advance(TimeSpan.FromDays(2));
            await ProcessAsync();
            var recovery = await StatusAsync();
            Assert.Equal("Recovery", recovery.Phase);
            // A hold written in another scope defeats an already loaded worker revision.
            await using (var staleScope = app.Services.CreateAsyncScope())
            {
                var staleDb = staleScope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
                var staleCase = await staleDb.KycRetentionCases.SingleAsync(x => x.TenantId == tenantId);
                await ActAsync("hold");
                staleCase.ClaimDeletion(time.GetUtcNow().AddDays(2));
                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync());
            }
            await ActAsync("release");
            time.Advance(TimeSpan.FromDays(2));
            failure.RetainNext = true;
            await ProcessAsync();
            Assert.Equal("Deleting", (await StatusAsync()).Phase);
            Assert.Equal("ProviderRetention", (await StatusAsync()).FailureCode);
            await app.Services.GetRequiredService<IKycDocumentSafetyProcessor>().ProcessBatchAsync(default);

            await using (var scanScope = app.Services.CreateAsyncScope())
            {
                var document = await scanScope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>().KycDocuments.SingleAsync(item => item.Id == documentId);
                Assert.Null(document.ErasedAt); // Retention must still retry physical purge before metadata erasure.
                Assert.Equal("erasure-recorded", document.SafetyReasonCode);
            }
            var unavailableHold = await ActResultAsync("hold");
            Assert.False(unavailableHold);
            time.Advance(TimeSpan.FromHours(2));
            failure.FailNext = new IOException("Synthetic storage outage");
            await ProcessAsync();
            Assert.Equal("StorageUnavailable", (await StatusAsync()).FailureCode);
            time.Advance(TimeSpan.FromHours(2));
            failure.FailNext = new RequestFailedException(409, "Synthetic storage protection");
            await ProcessAsync();
            Assert.Equal("StorageProtected", (await StatusAsync()).FailureCode);
            time.Advance(TimeSpan.FromHours(2));
            await ProcessAsync();
            Assert.Equal("Erased", (await StatusAsync()).Phase);
            var container = new BlobContainerClient(connection, "kyc-retention-tests", new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03));
            var key = $"kyc/{tenantId:N}/{documentId:N}";
            Assert.False((await container.GetBlobClient(key).ExistsAsync()).Value);

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
                var doc = await db.KycDocuments.SingleAsync(x => x.Id == documentId);
                Assert.NotNull(doc.ErasedAt);
                Assert.Equal("Deleted document", doc.FileName);
                Assert.Equal(0, doc.Length);
                Assert.Empty(await db.KycDocumentReviews.Where(x => x.DocumentId == documentId).ToArrayAsync());
                Assert.Contains(await db.KycRetentionAudit.Where(x => x.TenantId == tenantId).ToArrayAsync(), x => x.Action == "erased");
                // Simulate restoring older SQL and original-blob backups. Independent erasure intent still denies serving the content.
                await db.KycDocuments.Where(x => x.Id == documentId).ExecuteUpdateAsync(set => set.SetProperty(x => x.ErasedAt, (DateTimeOffset?)null));
                await db.KycRetentionCases.Where(x => x.TenantId == tenantId).ExecuteUpdateAsync(set => set.SetProperty(x => x.Phase, "Retained").SetProperty(x => x.Outcome, "Open"));
            }
            await container.GetBlobClient(key).UploadAsync(BinaryData.FromBytes(bytes));

            await using (var scope = app.Services.CreateAsyncScope())
            {
                Assert.False((await scope.ServiceProvider.GetRequiredService<IKycEvidenceService>().DownloadAsync(tenantId, documentId, default)).IsSuccess);
            }

            using (var forbiddenApproval = await vendor.PostAsJsonAsync($"/api/v1/control-plane/tenants/{tenantId}/approve",
                new
                {
                    EvidenceReference = "Synthetic attempt to reopen an erased case"
                }))
            {
                Assert.Equal(HttpStatusCode.UnprocessableEntity, forbiddenApproval.StatusCode);
            }

            await using (var scope = app.Services.CreateAsyncScope())
            {
                Assert.Null((await scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>().Tenants.SingleAsync(x => x.Id == tenantId)).KycApprovedAt);
            }
            // Unattached uploads are protected by a live reservation, then reconciled after its lease and grace have elapsed.
            var orphanId = Guid.CreateVersion7();
            var orphanKey = $"kyc/{tenantId:N}/{orphanId:N}";
            await container.GetBlobClient(orphanKey).UploadAsync(BinaryData.FromBytes(bytes));

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
                db.KycUploadReservations.Add(new KycUploadReservation { Id = orphanId, TenantId = tenantId, LeaseUntil = time.GetUtcNow().AddHours(1) });
                await db.SaveChangesAsync();
                await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().ReconcileOrphansAsync(default);
            }
            Assert.True((await container.GetBlobClient(orphanKey).ExistsAsync()).Value);
            time.Advance(TimeSpan.FromHours(2));
            failure.RetainNext = true;

            await using (var scope = app.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().ReconcileOrphansAsync(default);
            }

            Assert.False((await container.GetBlobClient(orphanKey).ExistsAsync()).Value);
            time.Advance(TimeSpan.FromHours(2));

            await using (var scope = app.Services.CreateAsyncScope())
            {
                await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().ReconcileOrphansAsync(default);
                var db = scope.ServiceProvider.GetRequiredService<ControlPlaneDBContext>();
                Assert.Equal("Erased", (await db.KycUploadReservations.SingleAsync(x => x.Id == orphanId)).State);
            }
        }
        finally
        {
            var active = (await vendor.GetFromJsonAsync<AppResponse<IReadOnlyList<KycRetentionPolicyResponse>>>($"{root}/policies"))!.Data![0];
            using var restoreDefault = await vendor.PostAsJsonAsync($"{root}/policies", new PublishKycRetentionPolicyRequest(active.Id,
                originalPolicy.Name, originalPolicy.ApprovedMonths, originalPolicy.ClosedDays, originalPolicy.InactiveDays,
                originalPolicy.RecoveryDays, originalPolicy.OrphanDays, originalPolicy.HoldReviewDays, "Restore general-business policy after synthetic acceptance"));

            restoreDefault.EnsureSuccessStatusCode();
        }

        async Task<KycRetentionStatusResponse> StatusAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().StatusAsync(tenantId, default)).Data!;
        }
        async Task<bool> ActResultAsync(string action)
        {
            var current = await StatusAsync();
            await using var scope = app.Services.CreateAsyncScope();
            return (await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().ActAsync(tenantId,
                new KycRetentionActionRequest(current.Revision, action, "Synthetic case operation"), default)).IsSuccess;
        }
        async Task ActAsync(string action)
        {
            Assert.True(await ActResultAsync(action), $"Synthetic retention action {action} failed.");
        }
        async Task ProcessAsync()
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IKycRetentionService>().ProcessAsync(tenantId, default);
        }
    }

    private sealed class RetentionClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
        public void Advance(TimeSpan duration)
        {
            _now += duration;
        }
    }
    private sealed class ProviderResult
    {
        public bool RetainNext
        {
            get; set;
        }
        public Exception? FailNext
        {
            get; set;
        }
    }
    private sealed class ControlledStorage(IKycEvidenceLifecycleStorage inner, ProviderResult result) : IKycEvidenceLifecycleStorage
    {
        public Task<bool> IsErasureRecordedAsync(string key, CancellationToken cancellationToken)
        {
            return inner.IsErasureRecordedAsync(key, cancellationToken);
        }
        public Task RecordErasureAsync(string key, CancellationToken cancellationToken)
        {
            return inner.RecordErasureAsync(key, cancellationToken);
        }
        public Task<bool> PurgeAsync(string key, CancellationToken cancellationToken)
        {
            if (result.FailNext is not null)
            {
                var failure = result.FailNext;
                result.FailNext = null;
                return Task.FromException<bool>(failure);
            }

            if (result.RetainNext)
            {
                result.RetainNext = false;
                return DeleteButReportProviderRetentionAsync(key, cancellationToken);
            }
            return inner.PurgeAsync(key, cancellationToken);
        }
        private async Task<bool> DeleteButReportProviderRetentionAsync(string key, CancellationToken cancellationToken)
        {
            await inner.PurgeAsync(key, cancellationToken);
            return false;
        }

        public Task<KycStoragePage> ListAsync(string? cursor, CancellationToken cancellationToken)
        {
            return inner.ListAsync(cursor, cancellationToken);
        }
    }
}

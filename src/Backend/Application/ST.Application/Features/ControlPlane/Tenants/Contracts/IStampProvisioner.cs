using System.Threading;
using System.Threading.Tasks;

namespace ST.Application.Features.ControlPlane.Tenants.Contracts;

public interface IStampProvisioner
{
    Task ProvisionIsolatedStampAsync(string tenantId, string stampId, string resourceGroup, string databaseProvider, Guid operationId, CancellationToken cancellationToken = default);
}

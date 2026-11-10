using ST.Domain.Shared.Contracts.Common;

namespace ST.Infrastructure.Contracts.Implementations.Common;
internal sealed class WorkspaceTenantOverride : IWorkspaceTenantOverride { public Guid? TenantId { get; set; } }

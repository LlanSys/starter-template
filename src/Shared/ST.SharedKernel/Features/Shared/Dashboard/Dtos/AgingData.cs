using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Dashboard.Dtos;

/// <summary>
/// Aging buckets for Pending Approval and Draft clients.
/// Based on OpenedOn — proxy for "time in current status" until status-change
/// timestamps are available via the audit log.
/// </summary>
public record AgingData(
    AgingBucket PendingApproval,
    AgingBucket Draft);
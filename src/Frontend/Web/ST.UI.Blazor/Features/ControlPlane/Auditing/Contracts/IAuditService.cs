using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ST.SharedKernel.Features.ControlPlane.Auditing.Dtos;
using ST.SharedKernel.Dtos.Common;

namespace ST.UI.Blazor.Features.ControlPlane.Auditing.Contracts;

internal interface IAuditService
{
    Task<AppResponse<ImpersonationRecordResponse>> StartImpersonationAsync(StartImpersonationRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> EndImpersonationAsync(Guid impersonationRecordId, CancellationToken cancellationToken = default);
    Task<AppResponse<IReadOnlyList<ImpersonationRecordDto>>> GetImpersonationRecordsAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
}

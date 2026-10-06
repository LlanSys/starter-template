using ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;
using ST.Domain.Features.ControlPlane.Auditing.Entities;
using ST.Persistence.Common;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.ControlPlane.DataContext;

namespace ST.Persistence.Features.ControlPlane.Auditing.Repositories;

internal sealed class ImpersonationRecordRepository : Repository<ImpersonationRecord>, IImpersonationRecordRepository
{
    public ImpersonationRecordRepository(ControlPlaneDBContext context) : base(context)
    {
    }
}

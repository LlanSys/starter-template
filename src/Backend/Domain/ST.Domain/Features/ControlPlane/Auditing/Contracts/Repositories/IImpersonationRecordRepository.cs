using ST.Domain.Features.ControlPlane.Auditing.Entities;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.ControlPlane.Auditing.Contracts.Repositories;

public interface IImpersonationRecordRepository : IRepository<ImpersonationRecord>
{
}

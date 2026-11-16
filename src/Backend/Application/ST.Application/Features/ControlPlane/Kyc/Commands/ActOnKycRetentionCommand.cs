using MediatR;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Kyc.Dtos;
namespace ST.Application.Features.ControlPlane.Kyc.Commands;

public sealed record ActOnKycRetentionCommand(Guid TenantId, KycRetentionActionRequest Request) : IRequest<AppResponse<bool>>;

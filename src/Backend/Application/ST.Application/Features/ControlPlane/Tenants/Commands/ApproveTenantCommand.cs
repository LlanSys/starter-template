using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;
using System;

namespace ST.Application.Features.ControlPlane.Tenants.Commands;

public record ApproveTenantCommand(Guid TenantId) : IRequest<AppResponse<TenantResponse>>;

using ST.SharedKernel.Features.IAM.Users.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.Commands;

public sealed record VerifyOtpCommand(VerifyOtpRequest Request) : IRequest<AppResponse<VerifyOtpResponse>>;

using MediatR;
using ST.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Notifications.Dtos;

public record SendEmailRequest : IRequest<AppResponse<SendEmailResponse>>
{
    public string? To { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? MemberName { get; set; }
    public string? MemberNumber { get; set; }
}

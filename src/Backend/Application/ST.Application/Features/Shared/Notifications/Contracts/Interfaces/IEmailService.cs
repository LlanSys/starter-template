using ST.SharedKernel.Features.Shared.Notifications.Dtos;
using ST.SharedKernel.Dtos.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Shared.Notifications.Contracts.Interfaces;

public interface IEmailService
{
    Task<AppResponse<SendEmailResponse>> SendEmailAsync(SendEmailRequest sendEmailRequest, CancellationToken cancellationToken);
}

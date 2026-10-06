using ST.Domain.Features.IAM.Users.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface ISmsComposer
{
    Task<string> ComposePasswordResetSmsAsync(RequestPasswordResetEvent evt);
}

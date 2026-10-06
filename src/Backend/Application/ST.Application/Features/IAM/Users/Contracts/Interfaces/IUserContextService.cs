using ST.Application.Features.IAM.Users.Contracts.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public interface IUserContextService
{
    /// <summary>
    /// Resolves the full identity context for the currently authenticated user.
    /// Reads from JWT claims — no DB call.
    /// </summary>
    UserIdentityContext GetCurrentContext();

}

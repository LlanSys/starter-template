using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Dtos.Common;

public record class AppRequest(
    Guid TenantId,
    string UserId
)
{
    protected AppRequest() : this(Guid.Empty, string.Empty)
    {
    }
}

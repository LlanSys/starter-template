using Microsoft.AspNetCore.Identity;

namespace ST.Infrastructure.Features.IAM.Authorization;

internal sealed class IamGrantUpdateRejectedException : Exception
{
    private IamGrantUpdateRejectedException() : base("Identity rejected an IAM grant update.")
    {
    }

    public static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new IamGrantUpdateRejectedException();
        }
    }
}

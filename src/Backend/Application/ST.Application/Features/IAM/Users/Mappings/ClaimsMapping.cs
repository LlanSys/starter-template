using ST.SharedKernel.Features.IAM.Users.Dtos;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ST.Application.Features.IAM.Users.Mappings;

public static class ClaimsMapping
{
    public static ClaimResponse ToClaimResponse(this Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        return new ClaimResponse(claim.Type, claim.Value);
    }

    public static Claim ToClaim(this ClaimResponse claimResponse)
    {
        ArgumentNullException.ThrowIfNull(claimResponse);
        return new Claim(claimResponse.Type, claimResponse.Value);
    }

    public static List<ClaimResponse> ToClaimResponses(this IEnumerable<Claim> claims) => [.. claims.Select(c => c.ToClaimResponse())];

    public static List<Claim> ToClaimList(this IEnumerable<ClaimResponse> claimDtos) => [.. claimDtos.Select(c => c.ToClaim())];
}

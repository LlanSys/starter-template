using System.Text.Json;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record RegisterPasskeyRequest(JsonElement AttestationResponse);

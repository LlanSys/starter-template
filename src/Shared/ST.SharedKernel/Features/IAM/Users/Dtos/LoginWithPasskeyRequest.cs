using System.Text.Json;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record LoginWithPasskeyRequest(string? Username, System.Guid CorrelationId, JsonElement AssertionResponse);

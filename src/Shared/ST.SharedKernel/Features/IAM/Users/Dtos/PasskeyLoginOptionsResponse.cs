using System;
using System.Text.Json;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record PasskeyLoginOptionsResponse(
    JsonElement Options,
    Guid CorrelationId);

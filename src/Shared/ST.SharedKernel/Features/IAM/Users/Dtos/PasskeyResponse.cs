using System;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record PasskeyResponse(
    Guid Id,
    string DisplayName,
    DateTimeOffset CreatedAt);

using System.ComponentModel.DataAnnotations;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record ConnectExternalIdentityRequest(
    [Required, StringLength(32)] string Provider,
    [Required, StringLength(128)] string Password);

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public record CurrentUserRequest(
    string UserId,
    string UserName,
    string Email,
    IList<string> Roles
);

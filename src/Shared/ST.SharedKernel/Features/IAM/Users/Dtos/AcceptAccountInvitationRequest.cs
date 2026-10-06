namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AcceptAccountInvitationRequest(string UserId, string Token, string Password, string ConfirmPassword);

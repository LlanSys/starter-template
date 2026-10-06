namespace ST.Application.Features.IAM.Users.Contracts.Interfaces;

public sealed record ProfilePictureFile(
    Stream Content,
    string ContentType,
    string FileName);

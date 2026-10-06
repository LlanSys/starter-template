using ST.Application.Behaviours;
using ST.Application.Features.IAM.Users.Commands;
using ST.Application.Features.IAM.Users.Validators;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Dtos.Utilities;
using ST.SharedKernel.Features.Shared.Common.Enums;
using ST.SharedKernel.Features.IAM.Users.Dtos;

using MediatR;

namespace ST.Tests.Unit.Application.Validation;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_Should_Return_AppResponse_ValidationFailure_When_Command_Is_Invalid()
    {
        var validator = new LoginCommandValidator();
        var behavior = new ValidationBehavior<LoginCommand, AppResponse<LoginResponse>>([validator]);
        var command = new LoginCommand(new TestLoginRequest("", "", false, null, ""));
        var nextCalled = false;

        var response = await behavior.Handle(
            command,
            _ =>
            {
                nextCalled = true;
                return Task.FromResult(AppResponses.Success("ok", CreateLoginResponse("token")));
            },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.False(response.IsSuccess);
        Assert.False(response.IsSuccess);

        var error = Assert.IsType<AppError>(response.Error);

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(ErrorCodes.Validation, error.Code);

        Assert.NotNull(error.ValidationErrors);
        Assert.NotEmpty(error.ValidationErrors);
    }

    [Fact]
    public async Task Handle_Should_Call_Next_When_Command_Is_Valid()
    {
        var validator = new LoginCommandValidator();
        var behavior = new ValidationBehavior<LoginCommand, AppResponse<LoginResponse>>([validator]);
        var command = new LoginCommand(new TestLoginRequest("admin@startertemplate.local", "Password1!", false, null, "device-1"));

        var response = await behavior.Handle(
            command,
            _ => Task.FromResult(AppResponses.Success("ok", CreateLoginResponse("token"))),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal("token", response.Data?.Token);
    }

    private static LoginResponse CreateLoginResponse(string token)
        => new(
            "user-1",
            "Template",
            "Admin",
            "admin@startertemplate.local",
            Requires2FA: false,
            RequiresEmailConfirmation: false,
            IsAuthenticated: true,
            token,
            "refresh",
            "session",
            DateTimeOffset.UtcNow,
            UserInfo: null,
            UserClaims: []);

    private sealed record TestLoginRequest(
        string UserName,
        string Password,
        bool RememberMe,
        string? ReturnUrl,
        string DeviceFingerprint)
        : LoginRequest(UserName, Password, RememberMe, ReturnUrl, DeviceFingerprint);
}

using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using ST.UI.Blazor.Features.IAM.Users.Utilities;
using ST.UI.Blazor.Features.Shared.Messaging;

namespace ST.UI.Blazor.Features.IAM.Users.Pages;

public partial class Profile
{

    private bool _isReady;
    private bool _isUploading;
    private const long MaxProfilePictureBytes = 10_485_760;

    private string DisplayName
    {
        get
        {
            var fullName = $"{AuthSession.CurrentUser?.FirstName} {AuthSession.CurrentUser?.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? AuthSession.CurrentUser?.Email ?? "Signed-in user" : fullName;
        }
    }

    private string Initials
    {
        get
        {
            var first = AuthSession.CurrentUser?.FirstName;
            var last = AuthSession.CurrentUser?.LastName;

            if (!string.IsNullOrWhiteSpace(first) && !string.IsNullOrWhiteSpace(last))
            {
                return $"{first[0]}{last[0]}".ToUpperInvariant();
            }

            var email = AuthSession.CurrentUser?.Email;
            return string.IsNullOrWhiteSpace(email) ? "ST" : email[..1].ToUpperInvariant();
        }
    }

    private string PrimaryRole => RoleDisplay.SelectPrimaryRole(AuthSession.CurrentUser?.Roles, "Signed in");

    private string? ProfilePictureSource => AuthSession.ProfilePictureDataUri;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await AuthSession.InitializeAsync();

        if (!AuthSession.IsAuthenticated)
        {
            Navigation.NavigateTo("/iam/sign-in");
            return;
        }

        if (AuthSession.MfaEnrollmentRequired)
        {
            Snackbar.Add("Two-factor authentication setup is required before you can manage your profile.", Severity.Warning);
            Navigation.NavigateTo("/iam/security");
            return;
        }

        _isReady = true;
        StateHasChanged();
    }

    private async Task OnProfilePictureSelectedAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;

        if (file is null)
        {
            return;
        }

        if (file.Size > MaxProfilePictureBytes)
        {
            Snackbar.Add("Profile picture must be 10 MB or smaller.", Severity.Warning);
            return;
        }

        _isUploading = true;

        try
        {
            await using var stream = file.OpenReadStream(MaxProfilePictureBytes);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);

            var response = await AuthService.UpdateProfilePictureAsync(
                memoryStream.ToArray(),
                file.Name,
                file.ContentType);

            if (!response.IsSuccess)
            {
                Snackbar.Add(UserMessageSanitizer.Normalize(response.Message, "Profile picture could not be updated."), Severity.Error);
                return;
            }

            await AuthSession.RefreshAsync();
            Snackbar.Add("Profile picture updated.", Severity.Success);
        }
        finally
        {
            _isUploading = false;
        }
    }

    protected override void OnInitialized()
    {
        AuthSession.AuthenticationChanged += OnAuthenticationChanged;
    }

    private void OnAuthenticationChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        AuthSession.AuthenticationChanged -= OnAuthenticationChanged;
    }

}

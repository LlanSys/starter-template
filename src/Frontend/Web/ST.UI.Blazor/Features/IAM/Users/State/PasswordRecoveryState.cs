namespace ST.UI.Blazor.Features.IAM.Users.State;

internal sealed class PasswordRecoveryState
{
    private string? _email;
    private string? _transitionToken;

    public void Set(string email, string transitionToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(transitionToken);

        _email = email;
        _transitionToken = transitionToken;
    }

    public bool TryConsume(out string email, out string transitionToken)
    {
        email = _email ?? string.Empty;
        transitionToken = _transitionToken ?? string.Empty;

        _email = null;
        _transitionToken = null;

        return !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(transitionToken);
    }
}

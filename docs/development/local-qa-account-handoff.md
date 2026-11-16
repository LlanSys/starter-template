# Local QA account handoff

These are generated synthetic test identities for the local StarterTemplate Docker deployment. They are not production accounts and are unrelated to your existing administrator password or authenticator.

The account setup helper saves generated credentials and MFA enrollment material in `%LOCALAPPDATA%/StarterTemplate/OperatorQA/credentials.dpapi`, encrypted with Windows DPAPI to the current Windows user. The repository and QA archives contain no credentials, tokens or authenticator seeds. A different Windows user cannot decrypt this handoff.

From **PowerShell 7** in `E:\Repos\StarterTemplate`, list the available generated accounts:

```powershell
.\scripts\Get-LocalQaLogin.ps1 -List
```

Retrieve one account when you are ready to sign in:

```powershell
.\scripts\Get-LocalQaLogin.ps1 -Workspace default -Username qa.collector
.\scripts\Get-LocalQaLogin.ps1 -Workspace qa-alder-01 -Username qa.alder.admin
```

The command intentionally displays that QA account's password and current authenticator code in your own console. Sign in at http://localhost:8181/iam/sign-in with the returned workspace/username/password, then use the fresh code. Re-run the command if its 30-second code expires. Do not paste the output into chat, screenshots, tickets or QA result logs. Optionally use `-EnrollmentKey` to enroll the generated QA authenticator in your own authenticator app; handle that key as a credential.

Keep platform, client submission and tenant sessions separate. The two customer administrators now have distinct emails and usernames, with separate tenant-scoped identities, passwords and authenticators. The original shared-email isolation check remains historical acceptance evidence.

For your own independent acceptance use `output/pdf/allan-run` and `startertemplate-allan-run.zip`; all results there remain NOT RUN. Reuse the platform operators after fresh sign-in, then create the Allan workspaces from the checklist. Their future customer credentials belong in your password manager unless separately captured by an authorized local QA setup run.

Keep the established two QA tenants as successful baselines. Perform expiry/deletion/restore exercises separately. After acceptance, deactivate test operators through the protected product screens and remove the private handoff if it is no longer needed. Never migrate this local QA credential store into cloud deployment configuration.

## Distinct customer administrators (15 November 2026 follow-up)

The current live synthetic administrators are Taylor Reed, `qa.alder.admin`, email `taylor.reed@alderfield.example.test`, and Jordan Ellis, `qa.brook.admin`, email `jordan.ellis@brookmere.example.test`. Passwords, account IDs, roles and enrolled authenticators are preserved. The encrypted local handoff was rekeyed to these usernames; the credential helper retains its qa-only restriction. The earlier shared-email scenario remains historical QA evidence.

```powershell
Set-Location E:\Repos\StarterTemplate
.\scripts\Get-LocalQaLogin.ps1 -Workspace qa-alder-01 -Username qa.alder.admin
.\scripts\Get-LocalQaLogin.ps1 -Workspace qa-brook-02 -Username qa.brook.admin
```

Use the returned credentials only in your local browser. Company entry addresses and configuration are in [tenant branding and sign-in](tenant-branding-and-sign-in.md#company-app-addresses). Profile and the account menu show the person; the company brand shows the tenant. Invitation/setup history records the original recipient at the time of issuance and is not rewritten as current profile data.

# Pull Request Guidelines

Pull requests should explain the outcome of the change, not just list files.
Keep the description short enough to review quickly, but clear enough that a future maintainer can understand why the change exists.

## Recommended Structure

```md
## Summary
Describe the user-facing or architecture-facing outcome in one or two sentences.

## What Changed
- Group changes by behavior or capability.
- Prefer meaningful bullets over raw file lists.
- Mention important architecture, data, API, UI, or configuration changes.

## Notes
- Call out dependencies, follow-up work, migrations, feature flags, secrets, rollout concerns, or known limitations.
- If there are no special notes, write `None`.
```

## Title Convention

Use a concise, outcome-focused title:

```text
Complete IAM foundation and admin reference flows
```

Good PR titles usually start with a verb and name the capability being changed.

Examples:

- `Add dynamic menu authorization`
- `Harden refresh token rotation`
- `Align lookup catalogs with soft delete`
- `Add customer search and pagination`

## Review Expectations

- Keep PRs focused where practical.
- Explain any intentional tradeoffs.
- Document migration or configuration impact.
- Do not include secrets, local machine paths, generated build output, or screenshots containing sensitive data.

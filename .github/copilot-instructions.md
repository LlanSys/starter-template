# GitHub Copilot Instructions

`AGENTS.md` is the canonical source of truth for this repository. Follow it first.

Before writing code, debugging, or reviewing changes:

1. Read `AGENTS.md`.
2. Read `PLAN.md` and `PLAN_EXECUTION_STRATEGY.md` for architecture or phase work.
3. Read the relevant docs under `docs/architecture` or `docs/development`.
4. Update `AGENTS.md` and the relevant docs when a convention changes.

Core reminders:

- Target `.NET 10`; root namespace prefix is `BT`.
- Keep the modular monolith and bounded-context feature-folder structure.
- Do not break Clean Architecture dependencies.
- Keep Application free of Infrastructure/Persistence implementation details.
- Use API-mediated profile image access; do not expose direct blob URLs.
- Sanitize user-facing messages; never show exception type names, stack traces, provider errors, or connection strings.
- Use source-generated `LoggerMessage`; every `catch` must log or deliberately translate the error.
- Add FluentValidation validators for user-facing write requests.
- Respect tenant isolation, soft delete, audit actor, caching, and repository/query standards.
- Never commit secrets or scratch files.

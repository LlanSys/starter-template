# CI/CD Workflow and Troubleshooting

This repository protects `main`. Treat `main` as the integration/release branch, not the everyday development branch.

## Standard Team Cycle

1. Start from an updated `main`.

   ```powershell
   git switch main
   git pull origin main
   ```

2. Create a feature branch.

   ```powershell
   git switch -c feature/short-description
   ```

3. Make changes locally.

4. Run local guardrails before pushing.

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts\checks.ps1
   ```

5. Push the feature branch.

   ```powershell
   git push -u origin feature/short-description
   ```

6. Open a pull request into `main`.

7. Wait for GitHub Actions to pass.

8. Resolve review comments and conversations.

9. Merge the pull request.

10. Update local `main`.

    ```powershell
    git switch main
    git pull origin main
    ```

## Why Direct Pushes to Main Fail

Direct pushes to `main` are intentionally blocked by branch protection.

Typical error:

```text
GH006: Protected branch update failed for refs/heads/main.
- Changes must be made through a pull request.
- Required status check "Build and architecture tests" is expected.
```

This is not a build failure. It means GitHub rejected the push because `main` only accepts changes through a pull request.

Fix:

```powershell
git switch -c feature/short-description
git push -u origin feature/short-description
```

Then open a PR into `main`.

## GitHub Actions Failure Checklist

When a GitHub Actions run fails, check in this order.

1. Confirm which workflow failed.

   Open the failed run in GitHub Actions and note the workflow name, job name, and failing step.

2. Confirm GitHub is running the workflow file you expect.

   If a fix was made locally but not pushed/merged, GitHub will still run the old workflow from `main`.

3. Check whether the failure is from restore, build, or tests.

   Restore failures usually mean dependency, workload, SDK, or NuGet source issues.

   Build failures usually mean compile errors, missing references, or incompatible target frameworks.

   Test failures usually mean guardrails or behavioral tests intentionally blocked the change.

4. The solution contains backend and Blazor projects only. No mobile workloads are required.

5. If the error mentions a required check is expected, check branch protection.

   The required check name must match the GitHub Actions job name exactly.

6. If local checks pass but GitHub fails, compare configuration.

   Look for differences in `Debug` vs `Release`, SDK version, OS runner, environment variables, or target framework.

## Current Guardrails

The local guardrail script runs:

```powershell
dotnet build src\Backend\Api\ST.Api\ST.Api.csproj --no-restore
dotnet test tests\ST.Tests.Architecture\ST.Tests.Architecture.csproj --no-restore
```

The GitHub backend guardrail workflow runs the backend API build and architecture tests in `Release`.

## Workflow Naming Convention

Name workflow files by the boundary they protect.

Recommended examples:

```text
backend-architecture.yml  Required backend architecture/build guardrail.
backend-ci.yml            Backend Release build plus unit/integration tests.
frontend-ci.yml           Blazor Release build.
full-solution-ci.yml      Manual/scheduled orchestration across backend/frontend.
release.yml               Manual Release artifact packaging.
```

Avoid vague names like `ci.yml` once the repository has multiple platforms or deployment paths.

## Debug vs Release

Use `Debug` for fast local feedback and pre-push checks.

Use `Release` in GitHub checks that protect `main`, because `main` represents merge-ready code and should stay close to deployable build conditions.

## Bypassing Local Hooks

Bypassing local hooks should be rare. GitHub branch protection still applies.

```powershell
$env:SKIP_GUARDRAILS="1"
git push
```

Use this only when the local environment is blocked but the PR will still run GitHub checks.

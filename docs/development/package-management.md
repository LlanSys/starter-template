# NuGet package management

StarterTemplate restores from public nuget.org. It has no internal NuGet libraries or GitHub NuGet publishing workflow. The repository `nuget.config` clears inherited sources and source mappings, then enables only nuget.org with the public package mapping. No GitHub package token is needed for restore, build, tools or formatting.

GitHub Container Registry (GHCR) is separate: container publishing and its `packages: write` workflow permission remain supported.

## Central versions and .NET 10 policy

Directory.Packages.props owns direct package versions, including the shared analyzer. Project PackageReference items declare dependencies without Version attributes. Native central overrides are disabled; edit the central entry once for every consuming project. Transitive pinning remains disabled so an unrelated central entry does not force a dependency into another project's graph.

The initial central-management migration aligned Microsoft.Extensions.Configuration, System.Security.Cryptography.Xml and Cryptography.Pkcs to 10.0.15. MediatR, coverage and xUnit runner references use the highest version already present in the repaired solution. The reviewed standalone CBOR test dependency remains explicit. Package version numbers are independent of target frameworks, but this baseline deliberately keeps ASP.NET/EF, Microsoft.Extensions and System.Security.Cryptography on 10.x.

All project targets remain net10.0. The root SDK selector is lowercase global.json in Git and Docker, with stable .NET 10 and latestFeature roll-forward within 10.0. CI installs 10.0.x; application SDK/runtime container images use 10.0. Update installed runtimes and rebuild images as servicing patches are released.

## Restore and verify

Run from the repository root after completing package edits:

```powershell
./scripts/check-package-policy.ps1
./scripts/test-package-policy.ps1
dotnet restore StarterTemplate.slnx -p:CI=true
dotnet tool restore
dotnet build StarterTemplate.slnx --no-restore -c Release -p:UseSharedCompilation=false
dotnet test tests/ST.Tests.Unit/ST.Tests.Unit.csproj --no-restore -c Release
dotnet test tests/ST.Tests.Architecture/ST.Tests.Architecture.csproj --no-restore -c Release
```

In Visual Studio, use Manage NuGet Packages for Solution with nuget.org selected. Consolidate related references before restoring the solution. If active diagnostics still describe old package versions after an external repair, reload the solution and restore again. Updating projects one at a time cannot fix contradictory constraints elsewhere in the project graph.

## Compatible updates

- Keep the target framework at net10.0. Installing a newer SDK does not make net11.0-only packages compatible. In particular, Microsoft.AspNetCore.Mvc.Testing stays on the 10.x line until an intentional framework upgrade; the failed 11.0.0 install was rolled back.
- Update FluentValidation and FluentValidation.DependencyInjectionExtensions together across their direct references. Both references use their central entries. Do not add inline versions or VersionOverride metadata: native CPM and the source policy reject those overrides. The previous hidden override selected 12.1.1 even when the visible Version was 12.2.0.
- Respect exact dependency constraints for families such as Roslyn. Architecture syntax checks use the central Microsoft.CodeAnalysis.CSharp entry. The architecture project does not need EF Core Design; that unnecessary direct reference introduced Roslyn Workspaces 5.0.0 with exact 5.0.0 compiler constraints. Keep the actual design-time dependencies in persistence/API/tooling where they are used.
- The build-only formatter consumes the central Roslyn version without referencing runtime projects. RazorStyleVersion is a separate exact download pin in Directory.Packages.props, used by both PackageDownload and its assembly HintPath.
- Keep MassTransit on the approved exact 8.x shared pin in Directory.Packages.props. Treat Quartz major upgrades as scheduler/schema compatibility work; do not include either family in indiscriminate major-version updates.
- Keep vulnerability auditing enabled for transitive dependencies. Fix the dependency graph instead of suppressing NU1605, NU1608 or NU1902.

## Updating a package

1. In Visual Studio, use Manage NuGet Packages for Solution and select the compatible version explicitly. For ASP.NET/EF and the approved Microsoft families, choose 10.x. CPM-aware tooling updates Directory.Packages.props; project references stay unversioned.
2. Review every coupled package in the proposed group, its release notes and target-framework support. FluentValidation, HotChocolate and Quartz family versions must agree. Add a new package with one central PackageVersion and unversioned references in the projects that consume it.
3. Run the policy check, strict restore, build and tests above. Include provider, migration, auth or browser checks when the dependency affects those flows.
4. Review and merge the tested change. Rebuild/redeploy the relevant hosts or container images to actually load the updated dependency. Never select every available major version as one bulk update.

The source checker reads configuration without downloading packages. NuGet restore performs dependency resolution and current vulnerability auditing. In CI, dependency/pruning conflicts, audit failures, unavailable audit data and known vulnerabilities at every reported severity are errors; no audit warning is suppressed.

## Dependabot and CI

.github/dependabot.yml proposes weekly updates on Monday at 06:00 Africa/Nairobi, grouped by platform, validation, GraphQL, Azure, telemetry, tests/analysis and other dependencies. Security update PRs have their own group. All automatic major-version updates are ignored, and no auto-merge is configured. MassTransit, Quartz and RazorStyle are excluded from automatic changes, including patches, because their shared pins/schema/serializer/formatter compatibility need deliberate verification. Security alerts for those packages still need prompt manual assessment; the audit gate does not waive them.

The dependency-policy workflow checks source policy and its accepted/rejected fixtures, performs strict full-solution restore/audit, restores tools and runs formatter preservation fixtures. Backend/frontend/architecture workflows also run the source check. Root manifests and SDK/feed changes trigger the relevant build and image workflows, with existing deployment enablement switches preserved. Docker restore stages copy Directory.Packages.props before project restore.

After merging these files to the default branch, Dependabot can read the schedule and GitHub can run the new workflow. Ensure Dependabot security updates/alerts are enabled in repository settings, and require the `.NET 10 and dependency policy` status check in the branch ruleset after its first successful run. Local validation does not establish that those GitHub settings or a hosted run have been changed. No GitHub NuGet feed is required.

## Reported vulnerable dependencies

SharedKernel did not consume Microsoft.AspNetCore.Diagnostics. Removing that unused legacy package removes the obsolete WebUtilities chain without adding an ASP.NET framework dependency to SharedKernel. Its unused workstation-relative Logging.Abstractions assembly HintPath was also removed; package restore must not depend on a developer-specific cache folder.

The architecture test graph retains Fido2's standalone System.Formats.Cbor dependency, so it explicitly selects patched 9.0.24. Application hosts use the ASP.NET Core shared framework assembly instead; an extra runtime package pin would be pruned. Keep deployed and local .NET runtimes patched independently of NuGet. The local runtime verified during this repair is ASP.NET Core 10.0.15. See the [Microsoft CBOR advisory](https://github.com/advisories/GHSA-vmv3-wvjp-wqpj) and [NuGet pruning guidance](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1510).

## Workstation feed cleanup

The unused GitHub NuGet source, its source mapping/disabled-source entry and associated stored source credentials were removed from the user's `%APPDATA%/NuGet/NuGet.Config` during the local repair. Other workstation package sources and GitHub repository authentication were not changed. The repository's source isolation also makes downstream restores independent of inherited personal feeds. Reintroducing an internal feed later requires an explicit library ownership, source-mapping and CI authentication decision.

## Local repair verification on 2026-11-13

All 13 solution projects restored successfully using nuget.org. A final forced restore treated NU1510, NU1605, NU1608 and NuGet audit warnings as errors; every project's restore assets contained zero diagnostics. The direct/transitive vulnerability report covered all 13 projects, used only nuget.org and reported zero vulnerable packages. Local .NET tools restored successfully; the notification that dotnet-ef 11 exists is informational and is not a reason to move the net10.0 repository to 11.x tools.

The final full-solution Release build passed with existing source-analyzer warnings. All 217 unit tests, 74 architecture tests and 29 PlatformIamSignInTests passed. The integration suite used disposable SQL Server, Mailpit and Azurite instances and exercised real password/authenticator/JWT authorization paths. The existing Debug hosts were left running, so restart them after rebuilding in Visual Studio to load the repaired dependencies. Docker application images and cloud deployments were not refreshed by this package repair. Changes remain local and uncommitted.

## Central-management verification on 2026-11-13

All 13 projects restored under the CI audit policy with 87 central package versions. The full Release build passed with existing analyzer warnings. All 217 unit, 74 architecture and 29 real password/authenticator/JWT integration checks passed. The source policy accepted the active repository and two synthetic valid fixtures, and rejected 27 independently varied policy violations. The formatter's 19 preservation/idempotence fixtures passed. Dependabot and all six edited/new workflow files validated against their published schemas. A full direct/transitive package audit covered 13 projects, used only nuget.org and reported zero vulnerable packages.

Both Linux application images built with the central manifest and lowercase SDK selector, using separate validation tags. Inspection confirmed .NET/ASP.NET Core 10.0.15 in the API image. Running Visual Studio/Docker hosts were not restarted, and no cloud deployment, hosted workflow run, branch-rule change or repository security-setting change was made. The isolated validator and policy-fixture files were removed. The changes remain local and uncommitted; record the SDK file's case-only rename when committing.

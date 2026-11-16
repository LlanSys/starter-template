[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$checker = Join-Path $PSScriptRoot 'check-package-policy.ps1'
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $tempBase ('st-package-policy-' + [IO.Path]::GetRandomFileName())
$baseline = @{}
$baseline['Directory.Packages.props'] = @'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageVersionOverrideEnabled>false</CentralPackageVersionOverrideEnabled>
    <CentralPackageTransitivePinningEnabled>false</CentralPackageTransitivePinningEnabled>
    <MassTransitVersion>8.5.12</MassTransitVersion>
    <RazorStyleVersion>2.0.1</RazorStyleVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="FluentValidation" Version="12.2.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="12.2.0" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.14" />
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.15" />
    <PackageVersion Include="System.Security.Cryptography.Xml" Version="10.0.15" />
    <PackageVersion Include="MassTransit" Version="[$(MassTransitVersion)]" />
    <PackageVersion Include="Quartz" Version="4.6.2" />
  </ItemGroup>
</Project>
'@
$baseline['Directory.Build.props'] = @'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>
  <PropertyGroup Condition="'$(CI)' == 'true'">
    <WarningsAsErrors>NU1510;NU1605;NU1608;NU1900;NU1901;NU1902;NU1903;NU1904;NU1905</WarningsAsErrors>
  </PropertyGroup>
</Project>
'@
$baseline['global.json'] = '{"sdk":{"version":"10.0.201","rollForward":"latestFeature","allowPrerelease": false}}'
$baseline['.config/dotnet-tools.json'] = '{"version":1,"isRoot":true,"tools":{"dotnet-ef":{"version":"10.0.8","rollForward":false}}}'
$baseline['nuget.config'] = '<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>'
$baseline['ops/docker/api.Dockerfile'] = "FROM mcr.microsoft.com/dotnet/aspnet:10.0`nFROM mcr.microsoft.com/dotnet/sdk:10.0`n" + 'COPY ["Directory.Packages.props", "."]'
$baseline['ops/docker/blazor.Dockerfile'] = $baseline['ops/docker/api.Dockerfile']
$baseline['Fixture.csproj'] = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="FluentValidation" /></ItemGroup></Project>'
$baseline['.github/workflows/policy.yml'] = "with:`n  dotnet-version: 10.0.x`n"

function Write-FixtureFile([string]$Path, [string]$Text) {
    $target = Join-Path $fixtureRoot $Path
    [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
    [IO.File]::WriteAllText($target, $Text)
}

function Reset-Fixture {
    foreach ($path in $baseline.Keys) { Write-FixtureFile $path $baseline[$path] }
}

$cases = @(
    @{ Name = 'project retarget'; Path = 'Fixture.csproj'; From = 'net10.0'; To = 'net11.0'; Error = 'must target net10.0' },
    @{ Name = 'inline version'; Path = 'Fixture.csproj'; From = 'Include="FluentValidation"'; To = 'Include="FluentValidation" Version="12.2.0"'; Error = 'per-project version/override' },
    @{ Name = 'child override'; Path = 'Fixture.csproj'; From = '<PackageReference Include="FluentValidation" />'; To = '<PackageReference Include="FluentValidation"><VersionOverride>12.1.1</VersionOverride></PackageReference>'; Error = 'per-project version/override' },
    @{ Name = 'missing central version'; Path = 'Fixture.csproj'; From = 'Include="FluentValidation"'; To = 'Include="Unknown.Package"'; Error = 'without a central PackageVersion' },
    @{ Name = 'framework package 11'; Path = 'Directory.Packages.props'; From = 'Include="Microsoft.EntityFrameworkCore" Version="10.0.14"'; To = 'Include="Microsoft.EntityFrameworkCore" Version="11.0.0"'; Error = 'must stay on the approved' },
    @{ Name = 'configuration package 11'; Path = 'Directory.Packages.props'; From = 'Include="Microsoft.Extensions.Configuration" Version="10.0.15"'; To = 'Include="Microsoft.Extensions.Configuration" Version="11.0.0"'; Error = 'must stay on the approved' },
    @{ Name = 'crypto package 11'; Path = 'Directory.Packages.props'; From = 'Include="System.Security.Cryptography.Xml" Version="10.0.15"'; To = 'Include="System.Security.Cryptography.Xml" Version="11.0.0"'; Error = 'must stay on the approved' },
    @{ Name = 'floating version'; Path = 'Directory.Packages.props'; From = 'Include="FluentValidation" Version="12.2.0"'; To = 'Include="FluentValidation" Version="12.*"'; Error = 'explicit stable version' },
    @{ Name = 'prerelease version'; Path = 'Directory.Packages.props'; From = 'Include="FluentValidation" Version="12.2.0"'; To = 'Include="FluentValidation" Version="12.3.0-preview.1"'; Error = 'explicit stable version' },
    @{ Name = 'family version mismatch'; Path = 'Directory.Packages.props'; From = 'Include="FluentValidation.DependencyInjectionExtensions" Version="12.2.0"'; To = 'Include="FluentValidation.DependencyInjectionExtensions" Version="12.1.1"'; Error = 'inconsistent versions' },
    @{ Name = 'CPM disabled'; Path = 'Directory.Packages.props'; From = '<ManagePackageVersionsCentrally>true'; To = '<ManagePackageVersionsCentrally>false'; Error = 'Central Package Management must be enabled' },
    @{ Name = 'overrides enabled'; Path = 'Directory.Packages.props'; From = '<CentralPackageVersionOverrideEnabled>false'; To = '<CentralPackageVersionOverrideEnabled>true'; Error = 'overrides must be disabled' },
    @{ Name = 'MassTransit major'; Path = 'Directory.Packages.props'; From = '<MassTransitVersion>8.5.12'; To = '<MassTransitVersion>9.0.0'; Error = 'approved 8.x' },
    @{ Name = 'MassTransit unbounded'; Path = 'Directory.Packages.props'; From = 'Include="MassTransit" Version="[$(MassTransitVersion)]"'; To = 'Include="MassTransit" Version="8.5.12"'; Error = 'exact shared MassTransit pin' },
    @{ Name = 'Quartz major'; Path = 'Directory.Packages.props'; From = 'Include="Quartz" Version="4.6.2"'; To = 'Include="Quartz" Version="5.0.0"'; Error = 'reviewed Quartz 4.x' },
    @{ Name = 'audit disabled'; Path = 'Directory.Build.props'; From = '<NuGetAudit>true'; To = '<NuGetAudit>false'; Error = 'weakens NuGetAudit' },
    @{ Name = 'vulnerability suppression'; Path = 'Fixture.csproj'; From = '</PropertyGroup>'; To = '<NoWarn>NU1902</NoWarn></PropertyGroup>'; Error = 'suppresses a guarded NuGet diagnostic' },
    @{ Name = 'package-level suppression'; Path = 'Fixture.csproj'; From = 'Include="FluentValidation"'; To = 'Include="FluentValidation" NoWarn="NU1902"'; Error = 'suppresses a guarded NuGet diagnostic' },
    @{ Name = 'reset CI errors'; Path = 'Fixture.csproj'; From = '</PropertyGroup>'; To = '<WarningsAsErrors /></PropertyGroup>'; Error = 'resets the inherited CI warning errors' },
    @{ Name = 'SDK 11'; Path = 'global.json'; From = '10.0.201'; To = '11.0.100'; Error = 'select stable .NET 10' },
    @{ Name = 'SDK major roll-forward'; Path = 'global.json'; From = 'latestFeature'; To = 'latestMajor'; Error = 'select stable .NET 10' },
    @{ Name = 'SDK previews'; Path = 'global.json'; From = '"allowPrerelease": false'; To = '"allowPrerelease": true'; Error = 'select stable .NET 10' },
    @{ Name = 'EF tools 11'; Path = '.config/dotnet-tools.json'; From = '10.0.8'; To = '11.0.0'; Error = 'dotnet-ef must stay pinned' },
    @{ Name = 'private source'; Path = 'nuget.config'; From = 'https://api.nuget.org/v3/index.json'; To = 'https://nuget.pkg.github.com/example/index.json'; Error = 'isolated public-only' },
    @{ Name = 'CI SDK 11'; Path = '.github/workflows/policy.yml'; From = '10.0.x'; To = '11.0.x'; Error = 'must install a .NET 10 SDK' },
    @{ Name = 'container 11'; Path = 'ops/docker/api.Dockerfile'; From = 'dotnet/aspnet:10.0'; To = 'dotnet/aspnet:11.0'; Error = 'must use .NET 10 images' },
    @{ Name = 'missing Docker manifest'; Path = 'ops/docker/blazor.Dockerfile'; From = 'COPY ["Directory.Packages.props", "."]'; To = '# missing manifest'; Error = 'must copy the central manifest' }
)

try {
    Reset-Fixture
    & $checker -RepoRoot $fixtureRoot -Quiet
    # Old, ignored clone artifacts must not become part of the active package graph.
    Write-FixtureFile 'artifacts/old-clone/Old.csproj' '<Project><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup></Project>'
    & $checker -RepoRoot $fixtureRoot -Quiet
    foreach ($case in $cases) {
        Reset-Fixture
        $original = $baseline[$case.Path]
        if (-not $original.Contains($case.From)) { throw "Fixture no longer matches: $($case.Name)" }
        Write-FixtureFile $case.Path ($original.Replace($case.From, $case.To))
        $rejection = $null
        try { & $checker -RepoRoot $fixtureRoot -Quiet } catch { $rejection = $_.Exception.Message }
        if ($null -eq $rejection -or -not $rejection.Contains($case.Error)) {
            throw "Expected rejection for $($case.Name); got: $rejection"
        }
    }
    Write-Output "PASS: 2 accepted fixtures and $($cases.Count) policy rejection cases."
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolved.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) -or (Split-Path -Leaf $resolved) -notlike 'st-package-policy-*') { throw 'Unexpected fixture cleanup target.' }
    if (Test-Path -LiteralPath $resolved) {
        if (Get-ChildItem -LiteralPath $resolved -Recurse -Force -Attributes ReparsePoint) { throw 'Reparse point found; fixture cleanup stopped.' }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}

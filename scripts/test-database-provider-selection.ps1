[CmdletBinding()]
param(
    [ValidateSet('SqlServer', 'PostgreSql')]
    [string[]]$Provider = @('SqlServer', 'PostgreSql'),

    # Also run the integration suite (needs Docker for Testcontainers).
    [switch]$IncludeIntegrationTests,

    # Keep the generated fork folders for inspection.
    [switch]$KeepForks
)

<#
Template-only fork check: copies the template, runs rename-template.ps1 for each provider,
then builds the fork and runs its tests. Used by .github/workflows/template-fork-check.yml.
#>

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/TemplateFiles.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$results = [ordered]@{}

function Invoke-Checked([string]$Description, [scriptblock]$Command) {
    Write-Host "==> $Description"
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$Description failed with exit code $LASTEXITCODE." }
}

foreach ($name in $Provider) {
    $forkRoot = Join-Path $temporaryRoot ("ForkProbe-$name-" + [Guid]::NewGuid().ToString('N'))
    try {
        Write-Host "### Fork check: $name ($forkRoot)"
        foreach ($entry in Get-ChildItem -LiteralPath $repoRoot -Recurse -Force) {
            if (Test-TemplateExcludedPath $repoRoot $entry.FullName @((Join-Path $repoRoot 'ops/local/.env'))) { continue }
            $target = Join-Path $forkRoot (Get-TemplateRelativePath $repoRoot $entry.FullName)
            if ($entry.PSIsContainer) {
                [IO.Directory]::CreateDirectory($target) | Out-Null
            }
            else {
                [IO.Directory]::CreateDirectory((Split-Path -Parent $target)) | Out-Null
                Copy-Item -LiteralPath $entry.FullName -Destination $target
            }
        }

        & (Join-Path $forkRoot 'scripts/rename-template.ps1') -NewName 'Fork Probe' -NamespacePrefix FP -DatabaseProvider $name -SkipLocalPortAllocation -Confirm:$false

        $solution = Join-Path $forkRoot 'ForkProbe.slnx'
        Invoke-Checked "[$name] restore" { dotnet restore $solution }
        Invoke-Checked "[$name] build" { dotnet build $solution --configuration Release --no-restore }
        Invoke-Checked "[$name] unit tests" { dotnet test (Join-Path $forkRoot 'tests/FP.Tests.Unit/FP.Tests.Unit.csproj') --configuration Release --no-build }
        Invoke-Checked "[$name] architecture tests" { dotnet test (Join-Path $forkRoot 'tests/FP.Tests.Architecture/FP.Tests.Architecture.csproj') --configuration Release --no-build }
        if ($IncludeIntegrationTests) {
            Invoke-Checked "[$name] integration tests" { dotnet test (Join-Path $forkRoot 'tests/FP.Tests.Integration/FP.Tests.Integration.csproj') --configuration Release --no-build }
        }
        $results[$name] = 'PASS'
    }
    catch {
        $results[$name] = "FAIL: $($_.Exception.Message)"
    }
    finally {
        if (-not $KeepForks -and (Test-Path -LiteralPath $forkRoot) -and
            ([IO.Path]::GetFileName($forkRoot) -like 'ForkProbe-*')) {
            Remove-Item -LiteralPath $forkRoot -Recurse -Force
        }
    }
}

$results.GetEnumerator() | ForEach-Object { Write-Host ("{0,-12} {1}" -f $_.Key, $_.Value) }
if ($results.Values | Where-Object { $_ -ne 'PASS' }) {
    throw 'One or more provider forks failed.'
}

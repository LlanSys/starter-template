param(
    [switch]$SkipBuild,
    [switch]$SkipArchitecture,
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

function Invoke-GuardrailCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string[]]$Command
    )

    Write-Host $Description
    & $Command[0] @($Command | Select-Object -Skip 1)

    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

if (-not $SkipRestore) {
    Invoke-GuardrailCommand `
        -Description "Restoring API project..." `
        -Command @("dotnet", "restore", "src\Backend\Api\ST.Api\ST.Api.csproj")

    Invoke-GuardrailCommand `
        -Description "Restoring architecture test project..." `
        -Command @("dotnet", "restore", "tests\ST.Tests.Architecture\ST.Tests.Architecture.csproj")
}

if (-not $SkipBuild) {
    Invoke-GuardrailCommand `
        -Description "Running API build..." `
        -Command @("dotnet", "build", "src\Backend\Api\ST.Api\ST.Api.csproj", "--no-restore")
}

if (-not $SkipArchitecture) {
    Invoke-GuardrailCommand `
        -Description "Running architecture tests..." `
        -Command @("dotnet", "test", "tests\ST.Tests.Architecture\ST.Tests.Architecture.csproj", "--no-restore")
}

Write-Host "Local guardrails passed."

#You can run this script with the following command:
#powershell -NoProfile -ExecutionPolicy Bypass -File scripts\checks.ps1

#If you ever need to bypass the pre-push hook temporarily:
#git push --no-verify
#$env:SKIP_GUARDRAILS="1"
#git push

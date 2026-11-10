[CmdletBinding()]
param(
    [ValidateSet('SqlServer', 'PostgreSql')]
    [string]$Provider = 'SqlServer',
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$operation = if ($Apply) { 'local-upgrade' } else { 'local-status' }
$providerName = if ($Provider -ieq 'PostgreSql') { 'PostgreSql' } else { 'SqlServer' }

Push-Location $repoRoot
try {
    & dotnet run --project tools/ST.DatabaseTools -- $providerName $operation
    if ($LASTEXITCODE -ne 0) {
        throw 'The local database workflow failed. Review the diagnostics above.'
    }
}
finally {
    Pop-Location
}

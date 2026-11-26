[CmdletBinding()]
param(
    # Defaults to the first supported provider.
    [string]$Provider,
    [switch]$Apply,
    # With -Apply: create a missing (or never-migrated) local database from this checkout's baseline.
    [switch]$CreateMissing
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$operation = if ($Apply -and $CreateMissing) { 'local-ensure' } elseif ($Apply) { 'local-upgrade' } else { 'local-status' }
$supportedDatabaseProviders = @(
    # <db:SqlServer>
    'SqlServer'
    # </db:SqlServer>
    # <db:PostgreSql>
    'PostgreSql'
    # </db:PostgreSql>
)
if ([string]::IsNullOrWhiteSpace($Provider)) { $Provider = $supportedDatabaseProviders[0] }
$providerName = $supportedDatabaseProviders | Where-Object { $_ -ieq $Provider } | Select-Object -First 1
if (-not $providerName) {
    throw "Unsupported database provider '$Provider'. Supported: $($supportedDatabaseProviders -join ', ')."
}

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

[CmdletBinding()]
param(
    # <db:SqlServer>
    [switch]$IncludeSqlServer,
    # </db:SqlServer>
    # <db:PostgreSql>
    [switch]$IncludePostgreSql,
    # </db:PostgreSql>
    [switch]$IncludeSeq
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot 'ops/local/docker-compose.yml'
$envFile = Join-Path $repoRoot 'ops/local/.env'

if (-not (Test-Path -LiteralPath $envFile)) {
    throw "Local platform environment file was not found at $envFile."
}

$profiles = @()
# <db:SqlServer>
if ($IncludeSqlServer) { $profiles += @('--profile', 'database') }
# </db:SqlServer>
# <db:PostgreSql>
if ($IncludePostgreSql) { $profiles += @('--profile', 'postgres') }
# </db:PostgreSql>
if ($IncludeSeq) { $profiles += @('--profile', 'observability') }

$composeArguments = @('compose', '--env-file', $envFile, '-f', $composeFile)
$composeArguments += $profiles
$composeArguments += 'stop'

& docker @composeArguments
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose stop failed with exit code $LASTEXITCODE."
}

Write-Host 'Local platform containers stopped. Named volumes and local data were preserved.'

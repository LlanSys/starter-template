[CmdletBinding()]
param(
    [switch]$UseContainerSql,
    # Seq starts by default; -SkipSeq leaves it out. -IncludeSeq is accepted for older commands.
    [switch]$IncludeSeq,
    [switch]$SkipSeq,
    [switch]$SkipPull,
    [switch]$UseSharedLocalKeyRing
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot 'ops/local/.env'
$composeFile = Join-Path $repoRoot 'ops/local/docker-compose.yml'
$appsComposeFile = Join-Path $repoRoot 'ops/local/docker-compose.apps.yml'
$platformScript = Join-Path $repoRoot 'scripts/setup-local-platform.ps1'
$apiProject = Join-Path $repoRoot 'src/Backend/Api/ST.Api/ST.Api.csproj'

function New-LocalSecret {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $bytes = New-Object byte[] 32
        $rng.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    } finally {
        $rng.Dispose()
    }
}

function Read-EnvironmentFile([string]$path) {
    $values = [ordered]@{}
    foreach ($line in Get-Content -LiteralPath $path) {
        if ($line -match '^\s*([^#][^=]*)=(.*)$') {
            $values[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    return $values
}

function Set-EnvironmentValue([string]$path, [string]$key, [string]$value) {
    $lines = if (Test-Path -LiteralPath $path) {
        @(Get-Content -LiteralPath $path)
    } else {
        @()
    }

    $updated = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match "^\s*$([regex]::Escape($key))\s*=") {
            $lines[$index] = "$key=$value"
            $updated = $true
            break
        }
    }

    if (-not $updated) {
        $lines += "$key=$value"
    }

    $lines | Set-Content -LiteralPath $path -Encoding ascii
}

# -UseContainerSql runs the first supported provider's local database container.
$containerDatabaseProvider = $null
# <db:SqlServer>
if (-not $containerDatabaseProvider) { $containerDatabaseProvider = 'SqlServer' }
# </db:SqlServer>
# <db:PostgreSql>
if (-not $containerDatabaseProvider) { $containerDatabaseProvider = 'PostgreSql' }
# </db:PostgreSql>

$platformArguments = @{}
if ($UseContainerSql) {
    $platformArguments["Include$containerDatabaseProvider"] = $true
}
if ($SkipSeq) {
    $platformArguments.SkipSeq = $true
}
if ($SkipPull) {
    $platformArguments.SkipPull = $true
}

if ($UseSharedLocalKeyRing) {
    $platformArguments.UseSharedLocalKeyRing = $true
}

& $platformScript @platformArguments
if ($LASTEXITCODE -ne 0) {
    throw "Local platform setup failed with exit code $LASTEXITCODE."
}

$environment = Read-EnvironmentFile $envFile

if (-not $environment.Contains('APP_API_HOST_PORT')) {
    Set-EnvironmentValue $envFile 'APP_API_HOST_PORT' '8180'
}
if (-not $environment.Contains('APP_BLAZOR_HOST_PORT')) {
    Set-EnvironmentValue $envFile 'APP_BLAZOR_HOST_PORT' '8181'
}
if (-not $environment.Contains('APP_JWT_SECRET') -or [string]::IsNullOrWhiteSpace($environment.APP_JWT_SECRET)) {
    Set-EnvironmentValue $envFile 'APP_JWT_SECRET' (New-LocalSecret)
}
if (-not $environment.Contains('APP_IAM_TEMPORARY_PASSWORD') -or [string]::IsNullOrWhiteSpace($environment.APP_IAM_TEMPORARY_PASSWORD)) {
    Set-EnvironmentValue $envFile 'APP_IAM_TEMPORARY_PASSWORD' "StTemp!$(Get-Random -Minimum 100000 -Maximum 999999)"
}

$environment = Read-EnvironmentFile $envFile
$databaseName = $environment.LOCAL_DATABASE_NAME
if ([string]::IsNullOrWhiteSpace($databaseName)) {
    $databaseName = [IO.Path]::GetFileNameWithoutExtension($apiProject) -replace '\.Api$', ''
}
if ($databaseName -notmatch '^[A-Za-z][A-Za-z0-9_]*$') {
    throw 'LOCAL_DATABASE_NAME must start with a letter and contain only letters, digits or underscores.'
}

if ($UseContainerSql) {
    # <db:SqlServer>
    if ($containerDatabaseProvider -eq 'SqlServer') {
        $containerSqlConnection = "Server=sqlserver,1433;Database=$databaseName;User Id=sa;Password=$($environment.MSSQL_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    }
    # </db:SqlServer>
    # <db:PostgreSql>
    if ($containerDatabaseProvider -eq 'PostgreSql') {
        $containerSqlConnection = "Host=postgres;Port=5432;Database=$databaseName;Username=postgres;Password=$($environment.POSTGRES_PASSWORD)"
    }
    # </db:PostgreSql>
    Set-EnvironmentValue $envFile 'APP_SQL_CONNECTION_STRING' $containerSqlConnection
} elseif (-not $environment.Contains('APP_SQL_CONNECTION_STRING') -or
    [string]::IsNullOrWhiteSpace($environment.APP_SQL_CONNECTION_STRING) -or
    $environment.APP_SQL_CONNECTION_STRING -like '*replace-local-sql-password*') {
    # <db:SqlServer>
    $exampleConnection = "Server=host.docker.internal;Database=$databaseName;User Id=sa;Password=<your-local-password>;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True"
    # </db:SqlServer>
    # <db:PostgreSql>
    if (-not $exampleConnection) { $exampleConnection = "Host=host.docker.internal;Port=5432;Database=$databaseName;Username=postgres;Password=<your-local-password>" }
    # </db:PostgreSql>
    throw @"
APP_SQL_CONNECTION_STRING is missing from $envFile.

Set it to the database connection your app containers should use, for example:
APP_SQL_CONNECTION_STRING=$exampleConnection

Or rerun this script with -UseContainerSql to use the local database container.
"@
}

$composeArguments = @(
    'compose',
    '--env-file', $envFile,
    '-f', $composeFile,
    '-f', $appsComposeFile,
    '--profile', 'apps'
)

& (Join-Path $PSScriptRoot 'check-local-endpoints.ps1') -ComposeFiles @($composeFile, $appsComposeFile) -FocusSupplied

if ($UseContainerSql) {
    # The API container neither creates nor migrates its database; without this step a fresh clone's API
    # restarts forever on "database does not exist". Create the database from the baseline when it is
    # missing, otherwise apply only pending migrations (with history and model-drift checks). It uses the
    # host user-secrets connection that setup-local-platform.ps1 pointed at the same container database.
    & (Join-Path $PSScriptRoot 'update-local-database.ps1') -Provider $containerDatabaseProvider -Apply -CreateMissing
}

& docker @composeArguments up -d --build --wait
if ($LASTEXITCODE -ne 0) {
    throw "Local app deployment failed with exit code $LASTEXITCODE."
}

$environment = Read-EnvironmentFile $envFile
Write-Host ''
Write-Host 'Local app deployment is ready:'
Write-Host "  API:    http://localhost:$($environment.APP_API_HOST_PORT)"
Write-Host "  Scalar: http://localhost:$($environment.APP_API_HOST_PORT)/scalar/v1"
Write-Host "  UI:     http://localhost:$($environment.APP_BLAZOR_HOST_PORT)"
Write-Host "  Mail:   http://localhost:$($environment.MAILPIT_UI_HOST_PORT)"
if (-not $SkipSeq) {
    Write-Host "  Seq:    http://localhost:$($environment.SEQ_HOST_PORT)"
}

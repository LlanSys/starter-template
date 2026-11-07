[CmdletBinding()]
param(
    [switch]$UseContainerSql,
    [switch]$IncludeSeq,
    [switch]$SkipPull
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot 'ops/local/.env'
$composeFile = Join-Path $repoRoot 'ops/local/docker-compose.yml'
$appsComposeFile = Join-Path $repoRoot 'ops/local/docker-compose.apps.yml'
$platformScript = Join-Path $repoRoot 'scripts/setup-local-platform.ps1'

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

$platformArguments = @{}
if ($UseContainerSql) {
    $platformArguments.IncludeSqlServer = $true
}
if ($IncludeSeq) {
    $platformArguments.IncludeSeq = $true
}
if ($SkipPull) {
    $platformArguments.SkipPull = $true
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
if ($UseContainerSql) {
    $containerSqlConnection = "Server=sqlserver,1433;Database=ST;User Id=sa;Password=$($environment.MSSQL_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    Set-EnvironmentValue $envFile 'APP_SQL_CONNECTION_STRING' $containerSqlConnection
} elseif (-not $environment.Contains('APP_SQL_CONNECTION_STRING') -or
    [string]::IsNullOrWhiteSpace($environment.APP_SQL_CONNECTION_STRING) -or
    $environment.APP_SQL_CONNECTION_STRING -like '*replace-local-sql-password*') {
    throw @"
APP_SQL_CONNECTION_STRING is missing from $envFile.

Set it to the SQL Server connection your app containers should use, for example:
APP_SQL_CONNECTION_STRING=Server=host.docker.internal;Database=ST;User Id=sa;Password=<your-local-password>;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True

Or rerun this script with -UseContainerSql to use the Docker SQL Server profile.
"@
}

$composeArguments = @(
    'compose',
    '--env-file', $envFile,
    '-f', $composeFile,
    '-f', $appsComposeFile,
    '--profile', 'apps'
)

& (Join-Path $PSScriptRoot 'check-local-endpoints.ps1') -ComposeFiles @($composeFile, $appsComposeFile)

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
Write-Host '  Mail:   http://localhost:8030'

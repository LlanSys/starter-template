[CmdletBinding()]
param(
    # Set by the git hooks: report problems, but never fail the pull, merge, rebase or checkout.
    [switch]$FromGitHook
)

<#
Brings a running local Docker stack up to date with the current checkout. The .husky post-merge,
post-rewrite (rebase) and post-checkout (branch switch) hooks run it, so a pull or branch switch does
not leave the stack on old migrations or old app images. It can also be run by hand at any time.

  - The local database container, when running, gets pending migrations. A missing or never-migrated
    local database is created from this checkout's baseline. History gaps, unknown migrations and
    model drift still stop the update (see scripts/update-local-database.ps1).
  - The API and Blazor app containers, when they exist, are rebuilt from this checkout and restarted.

Nothing is started that was not already running. Set SKIP_LOCAL_STACK_REFRESH=1 to skip the hooks.
#>

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot 'ops/local/.env'
$composeFile = Join-Path $repoRoot 'ops/local/docker-compose.yml'
$appsComposeFile = Join-Path $repoRoot 'ops/local/docker-compose.apps.yml'

function Write-Skip([string]$Reason) {
    Write-Host "Local stack refresh skipped: $Reason"
}

function Get-ComposeServices([string]$Status) {
    $arguments = @(
        'compose', '--env-file', $envFile, '-f', $composeFile, '-f', $appsComposeFile,
        '--profile', 'apps', '--profile', 'database', '--profile', 'postgres', '--profile', 'observability',
        'ps', '--services'
    )
    if ($Status) {
        $arguments += @('--status', $Status)
    }
    else {
        $arguments += '--all'
    }

    $services = & docker @arguments 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose ps failed with exit code $LASTEXITCODE."
    }

    return @($services | Where-Object { $_ })
}

try {
    if (-not (Test-Path -LiteralPath $envFile)) {
        Write-Skip 'ops/local/.env is not configured. Run scripts/setup-local-platform.ps1 first.'
        return
    }

    & docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Skip 'Docker is not running.'
        return
    }

    $running = Get-ComposeServices 'running'
    $databaseProvider = $null
    # <db:SqlServer>
    if (-not $databaseProvider -and $running -contains 'sqlserver') {
        $databaseProvider = 'SqlServer'
    }
    # </db:SqlServer>
    # <db:PostgreSql>
    if (-not $databaseProvider -and $running -contains 'postgres') {
        $databaseProvider = 'PostgreSql'
    }
    # </db:PostgreSql>

    if ($databaseProvider) {
        Write-Host "Updating the local $databaseProvider database for this checkout..."
        & (Join-Path $PSScriptRoot 'update-local-database.ps1') -Provider $databaseProvider -Apply -CreateMissing
    }
    else {
        Write-Host 'No local database container is running; migrations were not checked.'
    }

    $appServices = @(Get-ComposeServices | Where-Object { $_ -in @('api', 'blazor') })
    if ($appServices.Count -eq 0) {
        Write-Host 'No local app containers exist; nothing to rebuild.'
        return
    }

    Write-Host "Rebuilding local app containers from this checkout: $($appServices -join ', ')..."
    & docker compose --env-file $envFile -f $composeFile -f $appsComposeFile --profile apps up -d --build --wait @appServices
    if ($LASTEXITCODE -ne 0) {
        throw "Rebuilding the app containers failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Local stack is up to date with this checkout.'
}
catch {
    if (-not $FromGitHook) {
        throw
    }

    Write-Host "Local stack refresh failed: $($_.Exception.Message)"
    Write-Host 'The Git operation itself completed. Fix the problem above, then run scripts/refresh-local-stack.ps1.'
}

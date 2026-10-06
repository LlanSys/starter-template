[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$envFile = Join-Path $repoRoot 'ops/local/.env'
$composeFile = Join-Path $repoRoot 'ops/local/docker-compose.yml'
$testProject = Join-Path $repoRoot 'tests/ST.Tests.Integration/ST.Tests.Integration.csproj'

if (-not (Test-Path -LiteralPath $envFile)) {
    throw 'Local platform is not configured. Run ./scripts/setup-local-platform.ps1 first.'
}

$environment = @{}
foreach ($line in Get-Content -LiteralPath $envFile) {
    if ($line -match '^\s*([^#][^=]*)=(.*)$') {
        $environment[$matches[1].Trim()] = $matches[2].Trim()
    }
}

foreach ($required in @('RABBITMQ_USER', 'RABBITMQ_PASSWORD')) {
    if ([string]::IsNullOrWhiteSpace($environment[$required])) {
        throw "Local RabbitMQ setting '$required' is missing from $envFile."
    }
}

& docker compose --env-file $envFile -f $composeFile up -d --wait rabbitmq
if ($LASTEXITCODE -ne 0) {
    throw "RabbitMQ startup failed with exit code $LASTEXITCODE."
}

$env:ST_RUN_RABBITMQ_TESTS = 'true'
$env:ST_RABBITMQ_HOST = 'localhost'
$env:ST_RABBITMQ_PORT = '5675'
$env:ST_RABBITMQ_VIRTUAL_HOST = '/'
$env:ST_RABBITMQ_USERNAME = $environment.RABBITMQ_USER
$env:ST_RABBITMQ_PASSWORD = $environment.RABBITMQ_PASSWORD

try {
    dotnet test $testProject --filter 'Category=ExternalRabbitMq' -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) {
        throw "RabbitMQ transport certification failed with exit code $LASTEXITCODE."
    }
}
finally {
    Remove-Item Env:ST_RUN_RABBITMQ_TESTS -ErrorAction SilentlyContinue
    Remove-Item Env:ST_RABBITMQ_PORT -ErrorAction SilentlyContinue
    Remove-Item Env:ST_RABBITMQ_HOST -ErrorAction SilentlyContinue
    Remove-Item Env:ST_RABBITMQ_VIRTUAL_HOST -ErrorAction SilentlyContinue
    Remove-Item Env:ST_RABBITMQ_USERNAME -ErrorAction SilentlyContinue
    Remove-Item Env:ST_RABBITMQ_PASSWORD -ErrorAction SilentlyContinue
}

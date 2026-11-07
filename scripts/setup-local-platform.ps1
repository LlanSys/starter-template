[CmdletBinding()]
param(
    [switch]$IncludeSqlServer,
    [switch]$IncludeSeq,
    [switch]$SkipPull
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot 'ops/local/docker-compose.yml'
$envFile = Join-Path $repoRoot 'ops/local/.env'
$apiProject = Join-Path $repoRoot 'src/Backend/Api/ST.Api/ST.Api.csproj'

function New-LocalSecret {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $bytes = New-Object byte[] 24
        $rng.GetBytes($bytes)
        return [BitConverter]::ToString($bytes).Replace("-", "")
    } finally {
        $rng.Dispose()
    }
}

function New-LocalBase64Secret {
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
    $values = @{}
    foreach ($line in Get-Content -LiteralPath $path) {
        if ($line -match '^\s*([^#][^=]*)=(.*)$') {
            $values[$matches[1].Trim()] = $matches[2].Trim()
        }
    }

    return $values
}

if (-not (Test-Path -LiteralPath $envFile)) {
    $rabbitPassword = New-LocalSecret
    $redisPassword = New-LocalSecret
    $sqlPassword = "St!$(New-LocalSecret)"

    @(
        'RABBITMQ_USER=stdev'
        "RABBITMQ_PASSWORD=$rabbitPassword"
        "REDIS_PASSWORD=$redisPassword"
        'REDIS_HOST_PORT=6382'
        "MSSQL_SA_PASSWORD=$sqlPassword"
        'AZURITE_ACCOUNT_NAME=stdevstorage'
        "AZURITE_ACCOUNT_KEY=$(New-LocalBase64Secret)"
    ) | Set-Content -LiteralPath $envFile -Encoding ascii
}

$environment = Read-EnvironmentFile $envFile
if (-not $environment.ContainsKey('SEQ_PASSWORD')) {
    Add-Content -LiteralPath $envFile -Value "SEQ_PASSWORD=$(New-LocalSecret)"
    $environment = Read-EnvironmentFile $envFile
}
if (-not $environment.ContainsKey('REDIS_HOST_PORT')) {
    Add-Content -LiteralPath $envFile -Value 'REDIS_HOST_PORT=6382'
    $environment = Read-EnvironmentFile $envFile
}

if (-not $environment.ContainsKey('AZURITE_ACCOUNT_NAME')) {
    Add-Content -LiteralPath $envFile -Value 'AZURITE_ACCOUNT_NAME=stdevstorage'
}
if (-not $environment.ContainsKey('AZURITE_ACCOUNT_KEY')) {
    Add-Content -LiteralPath $envFile -Value "AZURITE_ACCOUNT_KEY=$(New-LocalBase64Secret)"
}
$environment = Read-EnvironmentFile $envFile

$requiredKeys = @(
    'RABBITMQ_USER',
    'RABBITMQ_PASSWORD',
    'REDIS_PASSWORD',
    'REDIS_HOST_PORT',
    'MSSQL_SA_PASSWORD',
    'AZURITE_ACCOUNT_NAME',
    'AZURITE_ACCOUNT_KEY'
)
foreach ($key in $requiredKeys) {
    if ([string]::IsNullOrWhiteSpace($environment[$key])) {
        throw "Local platform setting '$key' is missing from $envFile."
    }
}

& (Join-Path $PSScriptRoot 'check-local-endpoints.ps1') -ComposeFiles $composeFile

$volumeNames = @(
    'startertemplate_rabbitmq-data',
    'startertemplate_redis-data',
    'startertemplate_seq-data',
    'startertemplate_azurite-data',
    'startertemplate_sqlserver-data'
)
foreach ($volumeName in $volumeNames) {
    & docker volume inspect $volumeName *> $null
    if ($LASTEXITCODE -ne 0) {
        & docker volume create $volumeName *> $null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not create Docker volume '$volumeName'."
        }
    }
}

dotnet user-secrets set 'Messaging:Enabled' 'true' --project $apiProject
dotnet user-secrets set 'Messaging:Transport' 'RabbitMq' --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:Host' 'localhost' --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:Port' '5675' --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:VirtualHost' '/' --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:Username' $environment.RABBITMQ_USER --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:Password' $environment.RABBITMQ_PASSWORD --project $apiProject
dotnet user-secrets set 'CacheSettings:Provider' 'Redis' --project $apiProject
dotnet user-secrets set 'CacheSettings:Redis:ConnectionString' "localhost:$($environment.REDIS_HOST_PORT),password=$($environment.REDIS_PASSWORD),abortConnect=false" --project $apiProject
dotnet user-secrets set 'EmailSettings:Provider' 'LocalMailpit' --project $apiProject
dotnet user-secrets set 'EmailSettings:LocalMailpit:Host' 'localhost' --project $apiProject
dotnet user-secrets set 'EmailSettings:LocalMailpit:Port' '1030' --project $apiProject
dotnet user-secrets remove 'EmailSettings:Host' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:Port' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:Username' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:Password' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:EnableSsl' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:UseAuthentication' --project $apiProject *> $null
dotnet user-secrets remove 'SmtpSettings:Password' --project $apiProject *> $null
dotnet user-secrets set 'ProfileImageStorage:Provider' 'Azurite' --project $apiProject
$storageConnection = "DefaultEndpointsProtocol=http;AccountName=$($environment.AZURITE_ACCOUNT_NAME);AccountKey=$($environment.AZURITE_ACCOUNT_KEY);BlobEndpoint=http://127.0.0.1:10020/$($environment.AZURITE_ACCOUNT_NAME);QueueEndpoint=http://127.0.0.1:10021/$($environment.AZURITE_ACCOUNT_NAME);TableEndpoint=http://127.0.0.1:10022/$($environment.AZURITE_ACCOUNT_NAME);"
dotnet user-secrets set 'ProfileImageStorage:Azurite:ConnectionString' $storageConnection --project $apiProject
dotnet user-secrets set 'ProfileImageStorage:Azurite:ContainerName' 'profile-images' --project $apiProject

$profiles = @()
if ($IncludeSqlServer) {
    $profiles += @('--profile', 'database')
    $sqlConnection = "Server=localhost,14335;Database=ST;User Id=sa;Password=$($environment.MSSQL_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True;"
    dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $sqlConnection --project $apiProject
    dotnet user-secrets set 'DatabaseSettings:Provider' 'SqlServer' --project $apiProject
    foreach ($name in @('ControlPlaneConnection', 'IamConnection', 'SharedConnection', 'DefaultSqlConnection')) {
        dotnet user-secrets set "ConnectionStrings:$name" $sqlConnection --project $apiProject
    }
}
if ($IncludeSeq) {
    $profiles += @('--profile', 'observability')
}

& (Join-Path $PSScriptRoot 'initialize-payment-settings.ps1')

$composeArguments = @('compose', '--env-file', $envFile, '-f', $composeFile)
$composeArguments += $profiles
if (-not $SkipPull) {
    & docker @composeArguments pull
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Compose pull failed with exit code $LASTEXITCODE."
    }
}

& docker @composeArguments up -d --wait
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose startup failed with exit code $LASTEXITCODE."
}

Write-Host ''
Write-Host 'Local platform is ready:'
Write-Host '  RabbitMQ:  amqp://localhost:5675'
Write-Host '  Management: http://localhost:15675'
Write-Host "  Redis:      localhost:$($environment.REDIS_HOST_PORT)"
Write-Host '  Mailpit:    http://localhost:8030'
if ($IncludeSeq) { Write-Host '  Seq:        http://localhost:5344' }
Write-Host '  Azurite:    http://localhost:10020'
if ($IncludeSqlServer) { Write-Host '  SQL Server: localhost,14335' }

[CmdletBinding()]
param(
    # <db:SqlServer>
    [switch]$IncludeSqlServer,
    # </db:SqlServer>
    # <db:PostgreSql>
    [switch]$IncludePostgreSql,
    # </db:PostgreSql>
    # Seq starts by default; -SkipSeq leaves it out. -IncludeSeq is accepted for older commands.
    [switch]$IncludeSeq,
    [switch]$SkipSeq,
    [switch]$SkipPull,
    [switch]$UseSharedLocalKeyRing
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

# Generate each missing local credential. ops/local/.env may already exist with port settings
# written by allocate-local-ports.ps1, so credentials are added key by key.
if (-not (Test-Path -LiteralPath $envFile)) {
    New-Item -ItemType File -Path $envFile | Out-Null
}
$environment = Read-EnvironmentFile $envFile
$generatedSettings = [ordered]@{
    RABBITMQ_USER = 'stdev'
    RABBITMQ_PASSWORD = New-LocalSecret
    REDIS_PASSWORD = New-LocalSecret
    # <db:SqlServer>
    MSSQL_SA_PASSWORD = "St!$(New-LocalSecret)"
    # </db:SqlServer>
    AZURITE_ACCOUNT_NAME = 'stdevstorage'
    AZURITE_ACCOUNT_KEY = New-LocalBase64Secret
}
foreach ($key in $generatedSettings.Keys) {
    if ([string]::IsNullOrWhiteSpace($environment[$key])) {
        Add-Content -LiteralPath $envFile -Value "$key=$($generatedSettings[$key])"
    }
}

# Give this application its own local ports (registers the template's defaults for the template).
& (Join-Path $PSScriptRoot 'allocate-local-ports.ps1')

$environment = Read-EnvironmentFile $envFile
if (-not $environment.ContainsKey('SEQ_PASSWORD')) {
    Add-Content -LiteralPath $envFile -Value "SEQ_PASSWORD=$(New-LocalSecret)"
    $environment = Read-EnvironmentFile $envFile
}
if (-not $environment.ContainsKey('REDIS_HOST_PORT')) {
    Add-Content -LiteralPath $envFile -Value 'REDIS_HOST_PORT=6382'
    $environment = Read-EnvironmentFile $envFile
}

# <db:PostgreSql>
if (-not $environment.ContainsKey('POSTGRES_PASSWORD')) {
    Add-Content -LiteralPath $envFile -Value "POSTGRES_PASSWORD=$(New-LocalSecret)"
}
# </db:PostgreSql>
if (-not $environment.ContainsKey('AZURITE_ACCOUNT_NAME')) {
    Add-Content -LiteralPath $envFile -Value 'AZURITE_ACCOUNT_NAME=stdevstorage'
}
if (-not $environment.ContainsKey('AZURITE_ACCOUNT_KEY')) {
    Add-Content -LiteralPath $envFile -Value "AZURITE_ACCOUNT_KEY=$(New-LocalBase64Secret)"
}
$environment = Read-EnvironmentFile $envFile
$defaultPorts = [ordered]@{
    REDIS_HOST_PORT = 6382
    RABBITMQ_HOST_PORT = 5675
    RABBITMQ_MANAGEMENT_HOST_PORT = 15675
    MAILPIT_SMTP_HOST_PORT = 1030
    MAILPIT_UI_HOST_PORT = 8030
    SEQ_HOST_PORT = 5344
    AZURITE_BLOB_HOST_PORT = 10020
    AZURITE_QUEUE_HOST_PORT = 10021
    AZURITE_TABLE_HOST_PORT = 10022
    # <db:SqlServer>
    SQLSERVER_HOST_PORT = 14335
    # </db:SqlServer>
    # <db:PostgreSql>
    POSTGRES_HOST_PORT = 15435
    # </db:PostgreSql>
    CLAMAV_HOST_PORT = 13315
}
foreach ($key in $defaultPorts.Keys) {
    if (-not $environment.ContainsKey($key)) {
        Add-Content -LiteralPath $envFile -Value "$key=$($defaultPorts[$key])"
    }
}
$environment = Read-EnvironmentFile $envFile
foreach ($key in $defaultPorts.Keys) {
    $port = 0
    if (-not [int]::TryParse($environment[$key], [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
        throw "Local platform setting '$key' must be a port from 1 to 65535."
    }
}

$databaseName = $environment.LOCAL_DATABASE_NAME
if ([string]::IsNullOrWhiteSpace($databaseName)) {
    $databaseName = [IO.Path]::GetFileNameWithoutExtension($apiProject) -replace '\.Api$', ''
}
if ($databaseName -notmatch '^[A-Za-z][A-Za-z0-9_]*$') {
    throw 'LOCAL_DATABASE_NAME must start with a letter and contain only letters, digits or underscores.'
}

$requiredKeys = @(
    'RABBITMQ_USER',
    'RABBITMQ_PASSWORD',
    'REDIS_PASSWORD',
    'REDIS_HOST_PORT',
    # <db:SqlServer>
    'MSSQL_SA_PASSWORD',
    # </db:SqlServer>
    # <db:PostgreSql>
    'POSTGRES_PASSWORD',
    # </db:PostgreSql>
    'AZURITE_ACCOUNT_NAME',
    'AZURITE_ACCOUNT_KEY'
)
foreach ($key in $requiredKeys) {
    if ([string]::IsNullOrWhiteSpace($environment[$key])) {
        throw "Local platform setting '$key' is missing from $envFile."
    }
}

& (Join-Path $PSScriptRoot 'check-local-endpoints.ps1') -ComposeFiles $composeFile -FocusSupplied

$volumeNames = @(
    'startertemplate_rabbitmq-data',
    'startertemplate_redis-data',
    'startertemplate_seq-data',
    # <db:SqlServer>
    'startertemplate_sqlserver-data'
    # </db:SqlServer>
    # <db:PostgreSql>
    'startertemplate_postgres-data'
    # </db:PostgreSql>
    'startertemplate_azurite-data'
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
dotnet user-secrets set 'Messaging:RabbitMq:Port' $environment.RABBITMQ_HOST_PORT --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:VirtualHost' '/' --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:Username' $environment.RABBITMQ_USER --project $apiProject
dotnet user-secrets set 'Messaging:RabbitMq:Password' $environment.RABBITMQ_PASSWORD --project $apiProject
dotnet user-secrets set 'CacheSettings:Provider' 'Redis' --project $apiProject
dotnet user-secrets set 'CacheSettings:Redis:ConnectionString' "localhost:$($environment.REDIS_HOST_PORT),password=$($environment.REDIS_PASSWORD),abortConnect=false" --project $apiProject
dotnet user-secrets set 'EmailSettings:Provider' 'LocalMailpit' --project $apiProject
dotnet user-secrets set 'EmailSettings:LocalMailpit:Host' 'localhost' --project $apiProject
dotnet user-secrets set 'EmailSettings:LocalMailpit:Port' $environment.MAILPIT_SMTP_HOST_PORT --project $apiProject
dotnet user-secrets set 'KycDocumentSafety:Host' 'localhost' --project $apiProject
dotnet user-secrets set 'KycDocumentSafety:Port' $environment.CLAMAV_HOST_PORT --project $apiProject
dotnet user-secrets remove 'EmailSettings:Host' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:Port' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:Username' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:Password' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:EnableSsl' --project $apiProject *> $null
dotnet user-secrets remove 'EmailSettings:UseAuthentication' --project $apiProject *> $null
dotnet user-secrets remove 'SmtpSettings:Password' --project $apiProject *> $null
dotnet user-secrets set 'ProfileImageStorage:Provider' 'Azurite' --project $apiProject
$storageConnection = "DefaultEndpointsProtocol=http;AccountName=$($environment.AZURITE_ACCOUNT_NAME);AccountKey=$($environment.AZURITE_ACCOUNT_KEY);BlobEndpoint=http://127.0.0.1:$($environment.AZURITE_BLOB_HOST_PORT)/$($environment.AZURITE_ACCOUNT_NAME);QueueEndpoint=http://127.0.0.1:$($environment.AZURITE_QUEUE_HOST_PORT)/$($environment.AZURITE_ACCOUNT_NAME);TableEndpoint=http://127.0.0.1:$($environment.AZURITE_TABLE_HOST_PORT)/$($environment.AZURITE_ACCOUNT_NAME);"
dotnet user-secrets set 'ProfileImageStorage:Azurite:ConnectionString' $storageConnection --project $apiProject
dotnet user-secrets set 'ProfileImageStorage:Azurite:ContainerName' 'profile-images' --project $apiProject
dotnet user-secrets set 'KycDocuments:Provider' 'Azurite' --project $apiProject
dotnet user-secrets set 'KycDocuments:ConnectionString' $storageConnection --project $apiProject
dotnet user-secrets set 'KycDocuments:ContainerName' 'kyc-documents' --project $apiProject


$profiles = @()
# <db:SqlServer>
if ($IncludeSqlServer) {
    $profiles += @('--profile', 'database')
    $sqlConnection = "Server=localhost,$($environment.SQLSERVER_HOST_PORT);Database=$databaseName;User Id=sa;Password=$($environment.MSSQL_SA_PASSWORD);Encrypt=True;TrustServerCertificate=True;"
    dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $sqlConnection --project $apiProject
    dotnet user-secrets set 'DatabaseSettings:Provider' 'SqlServer' --project $apiProject
    foreach ($name in @('ControlPlaneConnection', 'IamConnection', 'SharedConnection', 'DefaultSqlConnection')) {
        dotnet user-secrets set "ConnectionStrings:$name" $sqlConnection --project $apiProject
    }
}
# </db:SqlServer>
# <db:PostgreSql>
if ($IncludePostgreSql) {
    $profiles += @('--profile', 'postgres')
    $postgresConnection = "Host=localhost;Port=$($environment.POSTGRES_HOST_PORT);Database=$databaseName;Username=postgres;Password=$($environment.POSTGRES_PASSWORD)"
    dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $postgresConnection --project $apiProject
    dotnet user-secrets set 'DatabaseSettings:Provider' 'PostgreSql' --project $apiProject
    foreach ($name in @('ControlPlaneConnection', 'IamConnection', 'SharedConnection')) {
        dotnet user-secrets set "ConnectionStrings:$name" $postgresConnection --project $apiProject
    }
}
# </db:PostgreSql>
if (-not $SkipSeq) {
    $profiles += @('--profile', 'observability')
    dotnet user-secrets set 'Serilog:WriteTo:1:Args:serverUrl' "http://localhost:$($environment.SEQ_HOST_PORT)" --project $apiProject
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

if ($UseSharedLocalKeyRing) {
    # Existing protected data must be checked before choosing a different key store.
    @{
        'DataProtection:UseExternalKeyStore' = 'true'
        'DataProtection:BlobKeyUri' = ''
        'DataProtection:RedisKeyRingConnectionString' = "localhost:$($environment.REDIS_HOST_PORT),password=$($environment.REDIS_PASSWORD),abortConnect=false"
        'DataProtection:RedisKeyRingKey' = 'StarterTemplate-DataProtection-Keys'
        'DataProtection:KeyEncryptionMode' = 'None'
    } | ConvertTo-Json | dotnet user-secrets set --project $apiProject
    if ($LASTEXITCODE -ne 0) { throw 'Local shared Data Protection configuration failed.' }
}

Write-Host ''
Write-Host 'Local platform is ready:'
Write-Host "  RabbitMQ:  amqp://localhost:$($environment.RABBITMQ_HOST_PORT)"
Write-Host "  Management: http://localhost:$($environment.RABBITMQ_MANAGEMENT_HOST_PORT)"
Write-Host "  Redis:      localhost:$($environment.REDIS_HOST_PORT)"
Write-Host "  Mailpit:    http://localhost:$($environment.MAILPIT_UI_HOST_PORT)"
if (-not $SkipSeq) { Write-Host "  Seq:        http://localhost:$($environment.SEQ_HOST_PORT)" }
Write-Host "  Azurite:    http://localhost:$($environment.AZURITE_BLOB_HOST_PORT)"
Write-Host "  KYC scanner: localhost:$($environment.CLAMAV_HOST_PORT) (private ClamAV; first signature load can take several minutes)"
# <db:SqlServer>
if ($IncludeSqlServer) { Write-Host "  SQL Server: localhost,$($environment.SQLSERVER_HOST_PORT)" }
# </db:SqlServer>
# <db:PostgreSql>
if ($IncludePostgreSql) { Write-Host "  PostgreSQL: localhost:$($environment.POSTGRES_HOST_PORT)" }
# </db:PostgreSql>

# Git hooks (.husky) are installed by the first local `dotnet build` or `dotnet restore` through
# Directory.Build.targets; HUSKY=0 skips that. Report it here so a fresh clone knows commits need it.
if (-not (Test-Path (Join-Path $repoRoot '.husky/_/husky.sh'))) {
    Write-Host ''
    Write-Host 'Git hooks are not installed yet. Run `dotnet build` once, or `dotnet tool restore; dotnet husky install`, before committing.'
}

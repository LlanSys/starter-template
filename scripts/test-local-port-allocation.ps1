[CmdletBinding()]
param()

<#
Fixture test for scripts/allocate-local-ports.ps1. Uses a temporary registry and ignores this
machine's Docker containers and listening ports (ST_PORT_SKIP_MACHINE), so results are deterministic.
#>

$ErrorActionPreference = 'Stop'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$workRoot = Join-Path $temporaryRoot ('PortProbe-' + [Guid]::NewGuid().ToString('N'))
$previous = @{ ST_PORT_REGISTRY = $env:ST_PORT_REGISTRY; ST_PORT_SKIP_MACHINE = $env:ST_PORT_SKIP_MACHINE }

function Assert-Condition([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }

function Write-FixtureFile([string]$root, [string]$relative, [string]$content) {
    $path = Join-Path $root $relative
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    [IO.File]::WriteAllText($path, $content, [Text.UTF8Encoding]::new($false))
}

function Read-Fixture([string]$root, [string]$relative) { [IO.File]::ReadAllText((Join-Path $root $relative)) }

function New-App([string]$name, [string]$product) {
    $root = Join-Path $workRoot $name
    [IO.Directory]::CreateDirectory((Join-Path $root 'scripts/lib')) | Out-Null
    Copy-Item (Join-Path $PSScriptRoot 'allocate-local-ports.ps1') (Join-Path $root 'scripts/allocate-local-ports.ps1')
    Copy-Item (Join-Path $PSScriptRoot 'lib/LocalPorts.ps1') (Join-Path $root 'scripts/lib/LocalPorts.ps1')
    Write-FixtureFile $root 'AGENTS.md' "- Product: $product"
    Write-FixtureFile $root 'src/Backend/Api/PP.Api/Properties/launchSettings.json' '{ "profiles": { "http": { "applicationUrl": "http://localhost:5257" }, "https": { "applicationUrl": "https://localhost:7229;http://localhost:5257" } } }'
    Write-FixtureFile $root 'src/Frontend/Web/PP.UI.Blazor/Properties/launchSettings.json' '{ "profiles": { "http": { "applicationUrl": "http://localhost:5206" }, "https": { "applicationUrl": "https://localhost:7149;http://localhost:5206" } } }'
    Write-FixtureFile $root 'src/Backend/Api/PP.Api/appsettings.json' '{ "AllowedOrigins": [ "https://localhost:7149", "http://localhost:5206" ], "CentralFrontendOrigins": [ "http://localhost:8181", "http://alder.localhost:8181" ], "Other": "localhost:72290" }'
    Write-FixtureFile $root 'src/Frontend/Web/PP.UI.Blazor/appsettings.json' '{ "BackendApi": { "BaseUrl": "https://localhost:7229/" } }'
    Write-FixtureFile $root 'scripts/Get-LocalQaLogin.ps1' "`$signIn = 'http://localhost:8181/iam/sign-in'"
    Write-FixtureFile $root 'ops/local/.env' "RABBITMQ_USER=probe`nREDIS_HOST_PORT=6382`n"
    return $root
}

function Invoke-Allocate([string]$root, [switch]$WhatIf, [switch]$Force) {
    & (Join-Path $root 'scripts/allocate-local-ports.ps1') -WhatIf:$WhatIf -Force:$Force *> (Join-Path $workRoot ("allocate-" + [Guid]::NewGuid().ToString('N') + '.log'))
}

function Read-EnvPort([string]$root, [string]$key) {
    $line = (Get-Content -LiteralPath (Join-Path $root 'ops/local/.env')) | Where-Object { $_ -match "^$key=" } | Select-Object -First 1
    if ($null -eq $line) { return 0 }
    return [int]($line -replace "^$key=", '')
}

function Read-Registry { Get-Content -Raw -LiteralPath $env:ST_PORT_REGISTRY | ConvertFrom-Json }

try {
    [IO.Directory]::CreateDirectory($workRoot) | Out-Null
    $env:ST_PORT_REGISTRY = Join-Path $workRoot 'registry/port-allocations.json'
    $env:ST_PORT_SKIP_MACHINE = '1'

    # 1. The template registers its defaults and changes nothing.
    $template = New-App 'Template' 'StarterTemplate'
    $templateApi = Read-Fixture $template 'src/Backend/Api/PP.Api/appsettings.json'
    Invoke-Allocate $template
    Assert-Condition ((Read-Fixture $template 'src/Backend/Api/PP.Api/appsettings.json') -ceq $templateApi) 'Template files changed.'
    $entry = (Read-Registry).allocations | Where-Object product -eq 'StarterTemplate'
    Assert-Condition ($entry.slot -eq 0 -and $entry.ports.API_HTTPS -eq 7229 -and $entry.ports.APP_BLAZOR_HOST_PORT -eq 8181) 'Template defaults were not registered as slot 0.'

    # 2. A clone with the defaults gets slot 1, written everywhere.
    $first = New-App 'First' 'FirstApp'
    Invoke-Allocate $first
    Assert-Condition ((Read-EnvPort $first 'APP_API_HOST_PORT') -eq 8190 -and (Read-EnvPort $first 'REDIS_HOST_PORT') -eq 6392 -and (Read-EnvPort $first 'AZURITE_TABLE_HOST_PORT') -eq 10032) 'First clone .env ports are wrong.'
    Assert-Condition ((Read-Fixture $first 'ops/local/.env').Contains('RABBITMQ_USER=probe')) 'Existing .env values were lost.'
    $apiLaunch = Read-Fixture $first 'src/Backend/Api/PP.Api/Properties/launchSettings.json'
    Assert-Condition ($apiLaunch.Contains('https://localhost:7239;http://localhost:5267') -and -not $apiLaunch.Contains('5257')) 'API launch ports were not updated.'
    Assert-Condition ((Read-Fixture $first 'src/Frontend/Web/PP.UI.Blazor/Properties/launchSettings.json').Contains('https://localhost:7159;http://localhost:5216')) 'Web launch ports were not updated.'
    $api = Read-Fixture $first 'src/Backend/Api/PP.Api/appsettings.json'
    Assert-Condition ($api.Contains('"https://localhost:7159", "http://localhost:5216"') -and $api.Contains('"http://localhost:8191"')) 'API settings URLs were not updated.'
    Assert-Condition ($api.Contains('alder.localhost:8181') -and $api.Contains('localhost:72290')) 'Unrelated localhost values were changed.'
    Assert-Condition ((Read-Fixture $first 'src/Frontend/Web/PP.UI.Blazor/appsettings.json').Contains('https://localhost:7239/')) 'Blazor API base URL was not updated.'
    Assert-Condition ((Read-Fixture $first 'scripts/Get-LocalQaLogin.ps1').Contains('http://localhost:8191/iam/sign-in')) 'Script URL was not updated.'

    # 3. A second clone avoids the first clone's registered block, even though nothing runs.
    $second = New-App 'Second' 'SecondApp'
    Invoke-Allocate $second
    Assert-Condition ((Read-EnvPort $second 'APP_API_HOST_PORT') -eq 8200) 'Second clone did not get slot 2.'

    # 4. Rerunning an allocated clone changes nothing.
    $before = (Get-FileHash (Join-Path $first 'src/Backend/Api/PP.Api/Properties/launchSettings.json')).Hash
    Invoke-Allocate $first
    Assert-Condition ((Get-FileHash (Join-Path $first 'src/Backend/Api/PP.Api/Properties/launchSettings.json')).Hash -eq $before) 'Rerun changed an allocated clone.'

    # 5. -WhatIf previews only.
    $preview = New-App 'Preview' 'PreviewApp'
    Invoke-Allocate $preview -WhatIf
    Assert-Condition ((Read-EnvPort $preview 'APP_API_HOST_PORT') -eq 0 -and -not ((Read-Registry).allocations | Where-Object product -eq 'PreviewApp')) 'WhatIf wrote changes.'

    # 6. A deleted clone frees its block.
    Remove-Item -LiteralPath $first -Recurse -Force
    $third = New-App 'Third' 'ThirdApp'
    Invoke-Allocate $third
    Assert-Condition ((Read-EnvPort $third 'APP_API_HOST_PORT') -eq 8190) 'Deleted clone did not release slot 1.'
    Assert-Condition (-not ((Read-Registry).allocations | Where-Object product -eq 'FirstApp')) 'Deleted clone stayed registered.'

    # 7. -Force moves a clone to the next free block and rewrites its URLs from the old ports.
    Invoke-Allocate $third -Force
    Assert-Condition ((Read-EnvPort $third 'APP_API_HOST_PORT') -eq 8190) 'Force should keep the first free block when the old one is only its own.'
    Invoke-Allocate $second -Force
    Assert-Condition ((Read-Fixture $second 'src/Frontend/Web/PP.UI.Blazor/appsettings.json').Contains('https://localhost:7249/')) 'Force re-allocation did not rewrite URLs from the previous ports.'

    Write-Host 'PASS: template registration, clone allocation and file updates, registry-based avoidance, idempotent rerun, WhatIf, release of deleted clones, and forced re-allocation.'
}
finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
    if ((Test-Path -LiteralPath $workRoot) -and ([IO.Path]::GetFileName($workRoot) -like 'PortProbe-*')) {
        Remove-Item -LiteralPath $workRoot -Recurse -Force
    }
}

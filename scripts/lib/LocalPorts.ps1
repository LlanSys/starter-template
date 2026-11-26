# Shared helpers for local port allocation (allocate-local-ports.ps1, check-local-endpoints.ps1).
# Dot-source this file; it defines functions only.
#
# Every application cloned from StarterTemplate gets a "slot". Slot 0 holds the template's defaults;
# slot n adds n * $script:StPortSlotStep to each default, which keeps the template's layout (for
# example Azurite's three consecutive ports) in every clone.

$script:StPortSlotStep = 10
$script:StPortMaxSlot = 40

# The template's own product name, split so rename-template.ps1 (which rewrites the product name in
# every text file) leaves it intact. Clones must still recognise the template and share its registry.
$script:StTemplateProduct = 'Starter' + 'Template'

# Infrastructure ports in ops/local/.env (consumed by docker-compose.yml / docker-compose.apps.yml).
$script:StEnvPortDefaults = [ordered]@{
    REDIS_HOST_PORT = 6382
    RABBITMQ_HOST_PORT = 5675
    RABBITMQ_MANAGEMENT_HOST_PORT = 15675
    MAILPIT_SMTP_HOST_PORT = 1030
    MAILPIT_UI_HOST_PORT = 8030
    SEQ_HOST_PORT = 5344
    AZURITE_BLOB_HOST_PORT = 10020
    AZURITE_QUEUE_HOST_PORT = 10021
    AZURITE_TABLE_HOST_PORT = 10022
    CLAMAV_HOST_PORT = 13315
    APP_API_HOST_PORT = 8180
    APP_BLAZOR_HOST_PORT = 8181
}
# <db:SqlServer>
$script:StEnvPortDefaults['SQLSERVER_HOST_PORT'] = 14335
# </db:SqlServer>
# <db:PostgreSql>
$script:StEnvPortDefaults['POSTGRES_HOST_PORT'] = 15435
# </db:PostgreSql>

# Visual Studio launch ports (Properties/launchSettings.json).
$script:StLaunchPortDefaults = [ordered]@{
    API_HTTP = 5257
    API_HTTPS = 7229
    WEB_HTTP = 5206
    WEB_HTTPS = 7149
}

function Get-StPortPlan([int]$Slot) {
    $plan = [ordered]@{}
    foreach ($table in @($script:StEnvPortDefaults, $script:StLaunchPortDefaults)) {
        foreach ($key in $table.Keys) { $plan[$key] = $table[$key] + ($Slot * $script:StPortSlotStep) }
    }
    return $plan
}

function Get-StPortRegistryPath {
    if ($env:ST_PORT_REGISTRY) { return $env:ST_PORT_REGISTRY }
    $base = [Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)
    return Join-Path (Join-Path $base $script:StTemplateProduct) 'port-allocations.json'
}

function ConvertTo-StRepositoryKey([string]$Path) {
    return [IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Read-StPortRegistry {
    $path = Get-StPortRegistryPath
    $entries = [System.Collections.Generic.List[object]]::new()
    if (Test-Path -LiteralPath $path) {
        $data = Get-Content -Raw -LiteralPath $path | ConvertFrom-Json
        foreach ($entry in @($data.allocations)) {
            if (-not $entry -or -not $entry.repository) { continue }
            # Forget applications whose folder no longer exists; their ports are free again.
            if (-not (Test-Path -LiteralPath $entry.repository)) { continue }
            $entries.Add($entry)
        }
    }
    return , $entries
}

function Write-StPortRegistry($Entries) {
    $path = Get-StPortRegistryPath
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    $json = [ordered]@{ version = 1; allocations = @($Entries) } | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText($path, $json, [Text.UTF8Encoding]::new($false))
}

function Read-StEnvFile([string]$Path) {
    $values = [ordered]@{}
    if (Test-Path -LiteralPath $Path) {
        foreach ($line in Get-Content -LiteralPath $Path) {
            if ($line -match '^\s*([^#][^=]*)=(.*)$') { $values[$matches[1].Trim()] = $matches[2].Trim() }
        }
    }
    return $values
}

function Set-StEnvValues([string]$Path, [System.Collections.IDictionary]$Values) {
    $lines = [System.Collections.Generic.List[string]]::new()
    if (Test-Path -LiteralPath $Path) { $lines.AddRange([string[]](Get-Content -LiteralPath $Path)) }
    foreach ($key in $Values.Keys) {
        $index = -1
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^\s*$([regex]::Escape($key))\s*=") { $index = $i; break }
        }
        $line = "$key=$($Values[$key])"
        if ($index -ge 0) { $lines[$index] = $line } else { $lines.Add($line) }
    }
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllLines($Path, $lines, [Text.ASCIIEncoding]::new())
}

function Get-StLaunchSettingsPath([string]$RepoRoot, [string]$Kind) {
    $pattern = if ($Kind -eq 'API') { '*.Api' } else { '*.UI.Blazor' }
    $base = if ($Kind -eq 'API') { 'src/Backend/Api' } else { 'src/Frontend/Web' }
    $project = Get-ChildItem -LiteralPath (Join-Path $RepoRoot $base) -Directory -Filter $pattern -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if (-not $project) { return $null }
    return Join-Path $project.FullName 'Properties/launchSettings.json'
}

function Get-StLaunchPorts([string]$LaunchSettingsPath) {
    $result = @{ http = $null; https = $null }
    if (-not $LaunchSettingsPath -or -not (Test-Path -LiteralPath $LaunchSettingsPath)) { return $result }
    $settings = Get-Content -Raw -LiteralPath $LaunchSettingsPath | ConvertFrom-Json
    foreach ($profile in $settings.profiles.PSObject.Properties) {
        foreach ($url in ("$($profile.Value.applicationUrl)" -split ';')) {
            if ($url -match '^(https?)://[^:/]+:(\d+)') {
                if (-not $result[$matches[1]]) { $result[$matches[1]] = [int]$matches[2] }
            }
        }
    }
    return $result
}

function Get-StCurrentPorts([string]$RepoRoot) {
    # Ports in effect today: .env values, falling back to the Compose defaults, and launch settings.
    $envValues = Read-StEnvFile (Join-Path $RepoRoot 'ops/local/.env')
    $current = [ordered]@{}
    foreach ($key in $script:StEnvPortDefaults.Keys) {
        $value = 0
        $current[$key] = if ([int]::TryParse("$($envValues[$key])", [ref]$value) -and $value -gt 0) { $value } else { $script:StEnvPortDefaults[$key] }
    }
    $api = Get-StLaunchPorts (Get-StLaunchSettingsPath $RepoRoot 'API')
    $web = Get-StLaunchPorts (Get-StLaunchSettingsPath $RepoRoot 'WEB')
    $current['API_HTTP'] = if ($api.http) { $api.http } else { $script:StLaunchPortDefaults.API_HTTP }
    $current['API_HTTPS'] = if ($api.https) { $api.https } else { $script:StLaunchPortDefaults.API_HTTPS }
    $current['WEB_HTTP'] = if ($web.http) { $web.http } else { $script:StLaunchPortDefaults.WEB_HTTP }
    $current['WEB_HTTPS'] = if ($web.https) { $web.https } else { $script:StLaunchPortDefaults.WEB_HTTPS }
    return $current
}

function Get-StNativeListeningPorts {
    # Ports held by processes on this machine (native services, Docker Desktop's published ports).
    $ports = [System.Collections.Generic.HashSet[int]]::new()
    if ($env:ST_PORT_SKIP_MACHINE) { return , $ports }
    if (Get-Command Get-NetTCPConnection -ErrorAction SilentlyContinue) {
        foreach ($connection in Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue) {
            [void]$ports.Add([int]$connection.LocalPort)
        }
    }
    else {
        Write-Warning 'Native listening ports cannot be read on this platform; only Docker and registered allocations are checked.'
    }
    return , $ports
}

function Get-StDockerReservations([string]$ExcludeRepoRoot) {
    # Host ports of every container (any state) plus every service of their Compose projects
    # (all profiles), excluding the supplied repository's own containers.
    $reserved = [System.Collections.Generic.List[object]]::new()
    if ($env:ST_PORT_SKIP_MACHINE) { return , $reserved }
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Warning 'Docker is not available; container port reservations were not checked.'
        return , $reserved
    }
    $ids = @(& docker ps -aq 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'Docker is not running; container port reservations were not checked.'
        return , $reserved
    }
    if (-not $ids.Count) { return , $reserved }
    $exclude = if ($ExcludeRepoRoot) { (ConvertTo-StRepositoryKey $ExcludeRepoRoot) + [IO.Path]::DirectorySeparatorChar } else { $null }
    $groups = @{}
    foreach ($container in (& docker inspect @ids | ConvertFrom-Json)) {
        $labels = $container.Config.Labels
        $directory = $labels.'com.docker.compose.project.working_dir'
        if ($exclude -and $directory -and ((ConvertTo-StRepositoryKey $directory) + [IO.Path]::DirectorySeparatorChar).StartsWith($exclude, [StringComparison]::OrdinalIgnoreCase)) { continue }
        foreach ($binding in $container.HostConfig.PortBindings.PSObject.Properties) {
            foreach ($hostBinding in $binding.Value) {
                if ($hostBinding.HostPort -match '^\d+$') {
                    $reserved.Add([pscustomobject]@{ Port = [int]$hostBinding.HostPort; Owner = "container $($container.Name.TrimStart('/'))" })
                }
            }
        }
        if ($directory -and $labels.'com.docker.compose.project.config_files') {
            if (-not $groups.ContainsKey($directory)) { $groups[$directory] = @() }
            $groups[$directory] = @($groups[$directory] + ($labels.'com.docker.compose.project.config_files' -split ',') | Select-Object -Unique)
        }
    }
    foreach ($directory in $groups.Keys) {
        $arguments = @('compose', '--profile', '*')
        $envFile = Join-Path $directory '.env'
        if (Test-Path -LiteralPath $envFile) { $arguments += @('--env-file', $envFile) }
        foreach ($file in $groups[$directory]) {
            if (Test-Path -LiteralPath $file) { $arguments += @('-f', $file) }
        }
        $json = & docker @arguments config --format json 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Skipping Compose group that cannot be resolved: $directory"
            continue
        }
        $config = $json | ConvertFrom-Json
        foreach ($service in $config.services.PSObject.Properties) {
            foreach ($mapping in $service.Value.ports) {
                if ("$($mapping.published)" -match '^\d+$') {
                    $reserved.Add([pscustomobject]@{ Port = [int]$mapping.published; Owner = "$($config.name)/$($service.Name)" })
                }
            }
        }
    }
    return , $reserved
}

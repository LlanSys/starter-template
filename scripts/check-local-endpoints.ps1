[CmdletBinding()]
param(
    [string[]]$ComposeFiles = @(),
    [string[]]$RepositoryPaths = @()
)
$ErrorActionPreference = 'Stop'
# Capture Compose JSON in memory only: it contains resolved local credentials.
$containerIds = @(& docker ps -aq)
if ($LASTEXITCODE -ne 0) { throw 'Docker inventory failed.' }
$containers = @()
if ($containerIds.Count) {
    $containers = @((& docker inspect @containerIds | ConvertFrom-Json))
    if ($LASTEXITCODE -ne 0) { throw 'Docker inspection failed.' }
}
$groups = @{}
foreach ($container in $containers) {
    $labels = $container.Config.Labels
    $directory = $labels.'com.docker.compose.project.working_dir'
    $files = $labels.'com.docker.compose.project.config_files'
    if ($directory -and $files) {
        if (-not $groups.ContainsKey($directory)) { $groups[$directory] = @() }
        $groups[$directory] = @($groups[$directory] + ($files -split ',') | Select-Object -Unique)
    }
}
foreach ($file in $ComposeFiles) {
    $fullPath = (Resolve-Path -LiteralPath $file).Path
    $directory = Split-Path -Parent $fullPath
    if (-not $groups.ContainsKey($directory)) { $groups[$directory] = @() }
    $groups[$directory] = @($groups[$directory] + $fullPath | Select-Object -Unique)
}
if (-not $groups.Count) { throw 'No Compose files found. Supply -ComposeFiles.' }
$ports = @()
$roots = @($RepositoryPaths)
foreach ($directory in $groups.Keys) {
    $files = @($groups[$directory] | Sort-Object)
    # Base definitions must precede application overrides.
    $files = @($files | Sort-Object { if ($_ -match 'compose.apps') { 1 } else { 0 } })
    $arguments = @('compose', '--profile', '*')
    $envFile = Join-Path $directory '.env'
    if (Test-Path -LiteralPath $envFile) { $arguments += @('--env-file', $envFile) }
    foreach ($file in $files) {
        if (-not (Test-Path -LiteralPath $file)) { throw "Missing Compose file: $file" }
        $arguments += @('-f', $file)
    }
    $json = & docker @arguments config --format json 2>$null
    if ($LASTEXITCODE -ne 0) { throw "Cannot resolve Compose configuration in $directory. Check local env values." }
    $config = $json | ConvertFrom-Json
    foreach ($service in $config.services.PSObject.Properties) {
        foreach ($mapping in $service.Value.ports) {
            if (-not $mapping.published) { continue }
            if ("$($mapping.published)" -notmatch '^\d+$') { throw 'Port ranges require individual allocations.' }
            $ports += [pscustomobject]@{
                Owner = "$($config.name)/$($service.Name)"
                Port = [int]$mapping.published
                Protocol = $(if ($mapping.protocol) { $mapping.protocol } else { 'tcp' })
            }
        }
    }
    $root = $directory
    while ($root -and -not (Test-Path -LiteralPath (Join-Path $root '.git'))) {
        $root = Split-Path -Parent $root
    }
    if ($root) { $roots += $root }
}
$roots = @($roots | Select-Object -Unique)
$secrets = @()
foreach ($root in $roots) {
    $source = Join-Path $root 'src'
    if (-not (Test-Path -LiteralPath $source)) { continue }
    foreach ($project in Get-ChildItem -LiteralPath $source -Recurse -Filter '*.csproj') {
        if ($project.FullName -match '[\\/](bin|obj)[\\/]') { continue }
        [xml]$xml = Get-Content -Raw -LiteralPath $project.FullName
        foreach ($id in $xml.SelectNodes('//UserSecretsId')) {
            $secrets += [pscustomobject]@{ Id = $id.InnerText; Owner = $project.FullName }
        }
        $launch = Join-Path $project.DirectoryName 'Properties/launchSettings.json'
        if (Test-Path -LiteralPath $launch) {
            $settings = Get-Content -Raw -LiteralPath $launch | ConvertFrom-Json
            $projectPorts = @()
            foreach ($profile in $settings.profiles.PSObject.Properties) {
                foreach ($url in ("$($profile.Value.applicationUrl)" -split ';')) {
                    if ($url -match '^https?://') { $projectPorts += ([uri]$url).Port }
                }
            }
            foreach ($port in ($projectPorts | Select-Object -Unique)) {
                $ports += [pscustomobject]@{ Owner = $project.FullName; Port = [int]$port; Protocol = 'tcp' }
            }
        }
    }
}
$failures = @()
foreach ($group in ($ports | Group-Object Port, Protocol)) {
    $owners = @($group.Group.Owner | Select-Object -Unique)
    if ($owners.Count -gt 1) { $failures += "Port collision $($group.Name): $($owners -join ', ')" }
}
foreach ($group in ($secrets | Group-Object Id)) {
    if (@($group.Group.Owner | Select-Object -Unique).Count -gt 1) {
        $failures += "Shared UserSecretsId $($group.Name): $($group.Group.Owner -join ', ')"
    }
}
# Detect containers created with obsolete bindings; do not recreate or delete them here.
foreach ($container in $containers) {
    $labels = $container.Config.Labels
    $project = $labels.'com.docker.compose.project'
    $service = $labels.'com.docker.compose.service'
    foreach ($binding in $container.HostConfig.PortBindings.PSObject.Properties) {
        foreach ($hostBinding in $binding.Value) {
            if (-not $hostBinding.HostPort) { continue }
            $owner = "$project/$service"
            $expected = @($ports | Where-Object Owner -eq $owner)
            if ($project -and $expected.Count -and [int]$hostBinding.HostPort -notin $expected.Port) {
                $failures += "Stale binding $($container.Name): $($hostBinding.HostPort); recreate with Compose."
            } elseif (-not $project) {
                Write-Warning "Standalone container $($container.Name) reserves port $($hostBinding.HostPort), state $($container.State.Status)."
            }
        }
    }
}
$ports | Sort-Object Port | Format-Table Owner, Port, Protocol -AutoSize
if ($failures.Count) {
    foreach ($failure in $failures) { Write-Error $failure -ErrorAction Continue }
    throw 'Local endpoint audit failed.'
}
Write-Host "PASS: $($ports.Count) distinct service/app allocations and $($secrets.Count) independent secret stores."
Write-Host 'Scope: discovered Compose groups (all profiles), supplied files, launch ports and secret-store identities. This does not validate credentials, database contents or external/native applications.'

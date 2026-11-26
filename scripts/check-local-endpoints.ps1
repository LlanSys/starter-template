[CmdletBinding()]
param(
    [string[]]$ComposeFiles = @(),
    [string[]]$RepositoryPaths = @(),
    [switch]$FocusSupplied
)
$ErrorActionPreference = 'Stop'
if ($FocusSupplied -and !$ComposeFiles.Count) {
    throw 'FocusSupplied requires at least one Compose file.'
}
$focusedOwners = @()
$focusedRoots = @()
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
$suppliedDirectories = @()
foreach ($file in $ComposeFiles) {
    $fullPath = (Resolve-Path -LiteralPath $file).Path
    $directory = Split-Path -Parent $fullPath
    $suppliedDirectories += $directory
    if (-not $groups.ContainsKey($directory)) { $groups[$directory] = @() }
    $groups[$directory] = @($groups[$directory] + $fullPath | Select-Object -Unique)
}
if (-not $groups.Count) { throw 'No Compose files found. Supply -ComposeFiles.' }
$composePorts = @{}
$git = Get-Command git -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
$repositoryRoots = @{}
$orderedDirectories = @($groups.Keys | Where-Object { $_ -notin $suppliedDirectories }) + $suppliedDirectories
foreach ($directory in ($orderedDirectories | Select-Object -Unique)) {
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
    if ($LASTEXITCODE -ne 0) {
        # In focused mode an unrelated project that cannot be resolved (for example a stale worktree
        # with missing env values) must not block the supplied application; report it and move on.
        if ($FocusSupplied -and $directory -notin $suppliedDirectories) {
            Write-Warning "Skipping unrelated Compose group that cannot be resolved: $directory. Its ports were not checked."
            continue
        }
        throw "Cannot resolve Compose configuration in $directory. Check local env values."
    }
    $config = $json | ConvertFrom-Json
    if ($directory -in $suppliedDirectories) {
        $focusedOwners += "$($config.name)/"
    }
    $root = $directory
    while ($root -and -not (Test-Path -LiteralPath (Join-Path $root '.git'))) {
        $root = Split-Path -Parent $root
    }

    $repositoryIdentity = $directory
    if ($root) {
        $repositoryIdentity = $root
        if ($git) {
            $commonDirectory = & $git.Source -C $root rev-parse --path-format=absolute --git-common-dir 2>$null
            if ($LASTEXITCODE -eq 0 -and $commonDirectory) {
                $repositoryIdentity = $commonDirectory
            }
        }
    }

    # A supplied Compose checkout takes precedence over a discovered linked worktree of the same project.
    $allocationKey = "$($config.name)|$repositoryIdentity"
    if ($root) {
        $repositoryRoots[$allocationKey] = $root
        if ($directory -in $suppliedDirectories) { $focusedRoots += $root }
    }
    $composePorts[$allocationKey] = @(foreach ($service in $config.services.PSObject.Properties) {
        foreach ($mapping in $service.Value.ports) {
            if (-not $mapping.published) { continue }
            if ("$($mapping.published)" -notmatch '^\d+$') { throw 'Port ranges require individual allocations.' }
            [pscustomobject]@{
                Owner = "$($config.name)/$($service.Name)"
                Port = [int]$mapping.published
                Protocol = $(if ($mapping.protocol) { $mapping.protocol } else { 'tcp' })
            }
        }
    })
}
$ports = @($composePorts.Values | ForEach-Object { $_ })
$roots = @($RepositoryPaths + @($repositoryRoots.Values) | Select-Object -Unique)
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
foreach ($container in $containers) {
    if ($container.Config.Labels.'com.docker.compose.project') { continue }
    foreach ($binding in $container.HostConfig.PortBindings.PSObject.Properties) {
        $protocol = $binding.Name.Split('/')[-1]
        foreach ($hostBinding in $binding.Value) {
            if (!$hostBinding.HostPort) { continue }
            $ports += [pscustomobject]@{
                Owner = "Standalone container $($container.Name)"
                Port = [int]$hostBinding.HostPort
                Protocol = $protocol
            }
        }
    }
}
# Applications recorded by allocate-local-ports.ps1 keep their ports even without containers.
. (Join-Path $PSScriptRoot 'lib/LocalPorts.ps1')
$knownRoots = @($roots + $focusedRoots | Where-Object { $_ } | ForEach-Object { ConvertTo-StRepositoryKey $_ })
foreach ($registered in (Read-StPortRegistry)) {
    $registeredRoot = ConvertTo-StRepositoryKey $registered.repository
    if ($registeredRoot -in $knownRoots) { continue }
    foreach ($property in $registered.ports.PSObject.Properties) {
        $ports += [pscustomobject]@{ Owner = "Registered $registeredRoot"; Port = [int]$property.Value; Protocol = 'tcp' }
    }
}
$failures = [Collections.Generic.List[string]]::new()
function Add-AllocationFailure([string]$message, [string[]]$owners) {
    $relevant = !$FocusSupplied
    foreach ($owner in $owners) {
        foreach ($prefix in $focusedOwners) {
            if ($owner.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { $relevant = $true }
        }
        foreach ($root in $focusedRoots) {
            if ($owner.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                $relevant = $true
            }
        }
    }
    if ($relevant) { $failures.Add($message) } else { Write-Warning $message }
}
foreach ($group in ($ports | Group-Object Port, Protocol)) {
    $owners = @($group.Group.Owner | Select-Object -Unique)
    if ($owners.Count -gt 1) {
        Add-AllocationFailure "Port collision $($group.Name): $($owners -join ', ')" $owners
    }
}
foreach ($group in ($secrets | Group-Object Id)) {
    if (@($group.Group.Owner | Select-Object -Unique).Count -gt 1) {
        Add-AllocationFailure "Shared UserSecretsId $($group.Name): $($group.Group.Owner -join ', ')" $group.Group.Owner
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
                Add-AllocationFailure "Stale binding $($container.Name): $($hostBinding.HostPort); recreate with Compose." @($owner)
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
if ($FocusSupplied) {
    Write-Host 'PASS: supplied Compose projects and hosts have no allocation conflicts with discovered reservations.'
} else {
    Write-Host "PASS: $($ports.Count) distinct service/app allocations and $($secrets.Count) independent secret stores."
}
Write-Host 'Scope: discovered Compose groups (all profiles), registered local allocations, supplied files, launch ports and secret-store identities. This does not validate credentials, database contents or external/native applications.'

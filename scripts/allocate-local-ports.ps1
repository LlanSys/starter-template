[CmdletBinding(SupportsShouldProcess)]
param(
    # Allocate a new block even if this application already has one.
    [switch]$Force
)

<#
Gives this application its own block of local ports so it can run next to other applications
cloned from StarterTemplate, and records the block in a per-user registry
(%LOCALAPPDATA%\StarterTemplate\port-allocations.json, or $env:ST_PORT_REGISTRY).

Default (ensure) mode:
  - already registered and unchanged: nothing to do;
  - the template itself: register its current (default) ports;
  - a clone still using the template's defaults, or whose ports are taken by another
    application: allocate the first free block;
  - otherwise: register the clone's current ports.

Taken ports come from: every Docker container (running or stopped) and every service of
their Compose projects (all profiles), ports listening on this machine, and the registry,
so applications that are not running still keep their ports.

Writes: ops/local/.env (port keys only), both launchSettings.json files, and the API/Blazor
settings and scripts that contain the local URLs.
#>

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/LocalPorts.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoKey = ConvertTo-StRepositoryKey $repoRoot
$envFile = Join-Path $repoRoot 'ops/local/.env'
$productMatch = [regex]::Match([IO.File]::ReadAllText((Join-Path $repoRoot 'AGENTS.md')), '(?m)^- Product: (.+)$')
$product = if ($productMatch.Success) { $productMatch.Groups[1].Value.Trim() } else { Split-Path -Leaf $repoRoot }
$isTemplate = $product -eq $script:StTemplateProduct

$registry = Read-StPortRegistry
$entry = $registry | Where-Object { (ConvertTo-StRepositoryKey $_.repository) -eq $repoKey } | Select-Object -First 1
$current = Get-StCurrentPorts $repoRoot
$defaults = Get-StPortPlan 0

function Test-SamePorts([System.Collections.IDictionary]$Left, $Right) {
    foreach ($key in $Left.Keys) {
        if ("$($Right.$key)" -ne "$($Left[$key])") { return $false }
    }
    return $true
}

# Ports owned by other applications.
$others = @{}
foreach ($other in $registry) {
    if ((ConvertTo-StRepositoryKey $other.repository) -eq $repoKey) { continue }
    foreach ($property in $other.ports.PSObject.Properties) { $others[[int]$property.Value] = "registered $($other.repository)" }
}
$dockerReservations = Get-StDockerReservations -ExcludeRepoRoot $repoRoot
foreach ($reservation in $dockerReservations) {
    if (-not $others.ContainsKey($reservation.Port)) { $others[$reservation.Port] = $reservation.Owner }
}

function Get-Conflicts([System.Collections.IDictionary]$Plan, [switch]$IncludeListening) {
    $conflicts = @()
    foreach ($key in $Plan.Keys) {
        $port = [int]$Plan[$key]
        if ($others.ContainsKey($port)) { $conflicts += "$key $port (used by $($others[$port]))" }
        elseif ($IncludeListening -and $script:listening.Contains($port)) { $conflicts += "$key $port (in use on this machine)" }
    }
    return , $conflicts
}

function Write-PortTable([System.Collections.IDictionary]$Plan, [string]$Title) {
    Write-Host $Title
    foreach ($key in $Plan.Keys) { Write-Host ("  {0,-30} {1}" -f $key, $Plan[$key]) }
}

function Save-Registry([System.Collections.IDictionary]$Plan, [int]$Slot) {
    $kept = @($registry | Where-Object { (ConvertTo-StRepositoryKey $_.repository) -ne $repoKey })
    $kept += [pscustomobject][ordered]@{
        repository = $repoKey
        product = $product
        slot = $Slot
        ports = [pscustomobject]$Plan
        updated = [DateTimeOffset]::UtcNow.ToString('o')
    }
    if ($PSCmdlet.ShouldProcess((Get-StPortRegistryPath), "Record local ports for $product")) {
        Write-StPortRegistry $kept
    }
}

# --- Decide. ---
$needsAllocation = $Force
if (-not $Force) {
    if ($entry -and (Test-SamePorts $current $entry.ports)) {
        $conflicts = Get-Conflicts $current
        if (-not $conflicts.Count) {
            Write-Host "Local ports for $product are registered and free (slot $($entry.slot))."
            return
        }
        if ($isTemplate) { throw "The template's default ports are claimed by another application: $($conflicts -join '; ')." }
        Write-Warning "Registered ports now conflict: $($conflicts -join '; '). Allocating a new block."
        $needsAllocation = $true
    }
    elseif ($isTemplate) {
        $conflicts = Get-Conflicts $current
        if ($conflicts.Count) { Write-Warning "Template ports are also claimed elsewhere: $($conflicts -join '; ')." }
        Save-Registry $current 0
        Write-Host 'Registered the template default ports (slot 0).'
        return
    }
    elseif (Test-SamePorts $defaults $current) {
        Write-Host "$product still uses the template's default ports; allocating its own block."
        $needsAllocation = $true
    }
    elseif ((Get-Conflicts $current).Count) {
        Write-Warning "Current ports conflict: $((Get-Conflicts $current) -join '; '). Allocating a new block."
        $needsAllocation = $true
    }
    else {
        $slot = if ($entry) { [int]$entry.slot } else { -1 }
        Save-Registry $current $slot
        Write-Host "Registered the current local ports for $product."
        return
    }
}

# --- Allocate the first free block. ---
$script:listening = Get-StNativeListeningPorts
$chosen = $null
for ($slot = 1; $slot -le $script:StPortMaxSlot; $slot++) {
    $plan = Get-StPortPlan $slot
    if ((Get-Conflicts $plan -IncludeListening).Count -eq 0) { $chosen = $slot; break }
}
if (-not $chosen) { throw "No free port block found in slots 1-$($script:StPortMaxSlot). Free some ports or remove old registrations." }
$plan = Get-StPortPlan $chosen

# --- Apply. ---
$alreadyConfigured = $entry -or (Test-Path -LiteralPath $envFile)
$envValues = [ordered]@{}
foreach ($key in $script:StEnvPortDefaults.Keys) { $envValues[$key] = $plan[$key] }
if ($PSCmdlet.ShouldProcess($envFile, 'Write local port settings')) {
    Set-StEnvValues $envFile $envValues
}

# Local URLs that embed the Visual Studio or Docker app ports.
$urlPortKeys = @('API_HTTP', 'API_HTTPS', 'WEB_HTTP', 'WEB_HTTPS', 'APP_API_HOST_PORT', 'APP_BLAZOR_HOST_PORT', 'SEQ_HOST_PORT')
$files = @(
    (Get-StLaunchSettingsPath $repoRoot 'API'),
    (Get-StLaunchSettingsPath $repoRoot 'WEB'),
    (Join-Path $repoRoot 'scripts/Get-LocalQaLogin.ps1')
)
foreach ($base in @('src/Backend/Api', 'src/Frontend/Web')) {
    $files += @(Get-ChildItem -LiteralPath (Join-Path $repoRoot $base) -Recurse -Filter 'appsettings*.json' -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | Select-Object -ExpandProperty FullName)
}
$portMap = @{}
foreach ($key in $urlPortKeys) {
    if ($current[$key] -ne $plan[$key]) { $portMap["$($current[$key])"] = "$($plan[$key])" }
}
foreach ($file in ($files | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique)) {
    $content = [IO.File]::ReadAllText($file)
    # One pass with an old-to-new map, so a port is never shifted twice. Only exact
    # localhost:<port> occurrences change (not e.g. tenant.localhost URLs in data).
    $updated = [regex]::Replace($content, '(?<![\w.-])localhost:(\d+)(?!\d)', {
        param($match)
        $old = $match.Groups[1].Value
        if ($portMap.ContainsKey($old)) { return 'localhost:' + $portMap[$old] }
        return $match.Value
    })
    if ($updated -ne $content -and $PSCmdlet.ShouldProcess($file, 'Update local URLs')) {
        [IO.File]::WriteAllText($file, $updated, [Text.UTF8Encoding]::new($false))
    }
}

Save-Registry $plan $chosen
Write-PortTable $plan "Local ports for $product (slot $chosen):"
Write-Host "Visual Studio: API https://localhost:$($plan.API_HTTPS)  Web https://localhost:$($plan.WEB_HTTPS)"
Write-Host "Docker:        API http://localhost:$($plan.APP_API_HOST_PORT)  Web http://localhost:$($plan.APP_BLAZOR_HOST_PORT)  Mail http://localhost:$($plan.MAILPIT_UI_HOST_PORT)  Seq http://localhost:$($plan.SEQ_HOST_PORT)"
if ($alreadyConfigured -and -not $WhatIfPreference) {
    Write-Host 'If this application already ran locally, rerun scripts/setup-local-platform.ps1 so its user-secrets use the new ports, and recreate its containers.'
}

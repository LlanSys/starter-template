[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidateSet('SqlServer', 'PostgreSql', 'Both')]
    [string]$Provider,

    # Skip the final scan for leftover references to the removed provider.
    [switch]$SkipLeftoverScan
)

<#
Reduces a StarterTemplate fork to one database provider, as described in
docs/architecture/database-provider-selection.md:
  1. deletes the removed provider's files (manifest 'paths'),
  2. removes its NuGet packages,
  3. drops its <db:Provider> marker blocks from shared files and strips all markers,
  4. applies the kept provider's configuration replacements,
  5. removes template-only tooling,
  6. fails if references to the removed provider remain.
'Both' keeps everything and removes template-only tooling.
#>

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/TemplateFiles.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'database-providers.json'
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json
$knownProviders = @($manifest.providers.PSObject.Properties.Name)
$excludedFiles = @((Join-Path $repoRoot 'ops/local/.env'))

if ($manifest.selectedProvider) {
    if ($manifest.selectedProvider -eq $Provider) {
        Write-Host "Database provider '$Provider' was already selected; nothing to do."
        return
    }
    throw "This repository was already reduced to '$($manifest.selectedProvider)'. Re-clone the template to choose '$Provider'."
}

$keep = if ($Provider -eq 'Both') { $knownProviders } else { @($Provider) }
$remove = @($knownProviders | Where-Object { $keep -notcontains $_ })

function Get-RepoEntries {
    Get-ChildItem -LiteralPath $repoRoot -Recurse -Force |
        Where-Object { -not (Test-TemplateExcludedPath $repoRoot $_.FullName $excludedFiles) }
}

function Remove-MatchingPaths([string[]]$Globs, [string]$Reason) {
    if (-not $Globs -or $Globs.Count -eq 0) { return }
    $regexes = $Globs | ForEach-Object { ConvertTo-TemplatePathRegex $_ }
    $entries = Get-RepoEntries | Sort-Object { $_.FullName.Length }
    $removed = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $entries) {
        $relative = Get-TemplateRelativePath $repoRoot $entry.FullName
        if ($removed | Where-Object { $relative.StartsWith($_ + '/') }) { continue }
        $candidate = if ($entry.PSIsContainer) { $relative + '/' } else { $relative }
        if (-not ($regexes | Where-Object { $candidate -match $_ })) { continue }
        if ($PSCmdlet.ShouldProcess($relative, "Remove ($Reason)")) {
            Remove-Item -LiteralPath $entry.FullName -Recurse -Force
        }
        $removed.Add($relative)
    }
    Write-Host ("{0}: {1} path(s)" -f $Reason, $removed.Count)
    $script:RemovedPaths.AddRange($removed)
}

$script:RemovedPaths = [System.Collections.Generic.List[string]]::new()

function Remove-LinksToRemovedPaths {
    # Turns Markdown links that point at removed files into plain text, so forks keep no broken links.
    if ($script:RemovedPaths.Count -eq 0) { return }
    $linkPattern = '\[(?<text>[^\]]+)\]\((?<target>[^)#\s]+)(?<anchor>#[^)\s]*)?\)'
    foreach ($file in (Get-TemplateTextFiles $repoRoot $excludedFiles | Where-Object { $_.Extension -eq '.md' })) {
        $directory = Split-Path -Parent (Get-TemplateRelativePath $repoRoot $file.FullName)
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $updated = [regex]::Replace($content, $linkPattern, {
            param($match)
            $target = $match.Groups['target'].Value
            if ($target -match '^[a-z][a-z0-9+.-]*:') { return $match.Value }
            $resolved = [System.IO.Path]::GetFullPath((Join-Path (Join-Path $repoRoot $directory) $target))
            $relative = Get-TemplateRelativePath $repoRoot $resolved
            $isRemoved = $script:RemovedPaths | Where-Object { $relative -eq $_ -or $relative.StartsWith($_ + '/') }
            if ($isRemoved) { return $match.Groups['text'].Value }
            return $match.Value
        })
        if ($updated -ne $content) { Write-TextFile $file.FullName $updated 'Unlink removed template-only documents' }
    }
}

function Write-TextFile([string]$Path, [string]$Content, [string]$Action) {
    if ($PSCmdlet.ShouldProcess((Get-TemplateRelativePath $repoRoot $Path), $Action)) {
        [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
    }
}

# 1. Provider files.
foreach ($name in $remove) {
    Remove-MatchingPaths @($manifest.providers.$name.paths) "remove $name files"
}

# 2. Packages.
$packageIds = @($remove | ForEach-Object { $manifest.providers.$_.packages })
if ($packageIds.Count -gt 0) {
    $packagePattern = '(?m)^[ \t]*<Package(?:Version|Reference)\s+Include="(?:' +
        (($packageIds | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')"[^\r\n]*\r?\n'
    $projectFiles = Get-RepoEntries | Where-Object {
        -not $_.PSIsContainer -and ($_.Extension -in @('.csproj', '.props', '.targets'))
    }
    foreach ($file in $projectFiles) {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $updated = [regex]::Replace($content, $packagePattern, '')
        if ($updated -ne $content) { Write-TextFile $file.FullName $updated 'Remove provider packages' }
    }
}

# 3. Marker blocks (always validated, even for 'Both').
$textFiles = @(Get-TemplateTextFiles $repoRoot $excludedFiles -IncludeProviderExtras)
# Files that document or implement the marker syntax are not processed.
$markerExclusionRegexes = @(
    @('scripts/lib/TemplateFiles.ps1', 'scripts/select-database-provider.ps1') + @($manifest.markerProcessingExclusions) |
        Where-Object { $_ } | ForEach-Object { ConvertTo-TemplatePathRegex $_ })
foreach ($file in $textFiles) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    if ($content -notmatch '<(?:/)?db:[A-Za-z]+>') { continue }
    $relative = Get-TemplateRelativePath $repoRoot $file.FullName
    if ($markerExclusionRegexes | Where-Object { $relative -match $_ }) { continue }
    $updated = Remove-ProviderMarkerBlocks -Content $content -Keep $keep -KnownProviders $knownProviders -Source $relative
    if ($Provider -eq 'Both') { continue }
    if ($updated -ne $content) { Write-TextFile $file.FullName $updated 'Apply database provider markers' }
}

# 4. Kept-provider configuration.
foreach ($name in $keep) {
    if ($Provider -eq 'Both') { break }
    foreach ($replacement in @($manifest.providers.$name.keepReplacements)) {
        if (-not $replacement) { continue }
        $pathRegex = ConvertTo-TemplatePathRegex $replacement.path
        foreach ($file in $textFiles) {
            if (-not (Test-Path -LiteralPath $file.FullName)) { continue }
            if ((Get-TemplateRelativePath $repoRoot $file.FullName) -notmatch $pathRegex) { continue }
            $content = [System.IO.File]::ReadAllText($file.FullName)
            $updated = [regex]::Replace($content, $replacement.pattern, $replacement.replacement)
            if ($updated -ne $content) { Write-TextFile $file.FullName $updated "Configure $name" }
        }
    }
}

# 5. Template tooling.
Remove-MatchingPaths @($manifest.templateOnly) 'remove template-only tooling'
if ($Provider -ne 'Both') {
    Remove-MatchingPaths @($manifest.markerTooling) 'remove provider marker tooling'
}
Remove-LinksToRemovedPaths

# Record the choice so a rerun cannot remove the kept provider.
$manifestText = [System.IO.File]::ReadAllText($manifestPath)
$recorded = [regex]::Replace($manifestText, '"selectedProvider"\s*:\s*null', ('"selectedProvider": "' + $Provider + '"'))
Write-TextFile $manifestPath $recorded 'Record selected database provider'

# 6. Leftover scan.
if ($Provider -eq 'Both' -or $SkipLeftoverScan -or $WhatIfPreference) {
    Write-Host "Database provider '$Provider' selected."
    return
}

$exclusionRegexes = @($manifest.leftoverScanExclusions | ForEach-Object { ConvertTo-TemplatePathRegex $_ })
$leftoverPattern = '(?i)' + (($remove | ForEach-Object { $manifest.providers.$_.leftoverPattern }) -join '|')
$leftovers = [System.Collections.Generic.List[string]]::new()
foreach ($file in (Get-TemplateTextFiles $repoRoot $excludedFiles -IncludeProviderExtras)) {
    $relative = Get-TemplateRelativePath $repoRoot $file.FullName
    if ($exclusionRegexes | Where-Object { $relative -match $_ }) { continue }
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
        $lineNumber++
        if ($line -match $leftoverPattern) { $leftovers.Add("${relative}:${lineNumber}: $($line.Trim())") }
    }
}
foreach ($entry in Get-RepoEntries) {
    $relative = Get-TemplateRelativePath $repoRoot $entry.FullName
    if ($entry.Name -match $leftoverPattern) { $leftovers.Add("${relative}: path name") }
}

if ($leftovers.Count -gt 0) {
    $leftovers | Select-Object -First 50 | ForEach-Object { Write-Host "  $_" }
    throw "Found $($leftovers.Count) reference(s) to the removed provider ($($remove -join ', ')). Add markers or manifest paths in the template."
}

Write-Host "Database provider '$Provider' selected; removed $($remove -join ', ')."

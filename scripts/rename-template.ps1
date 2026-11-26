[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9 ]*$')]
    [string]$NewName,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Z][A-Z0-9]*$')]
    [string]$NamespacePrefix,

    # SqlServer or PostgreSql removes the other provider completely; Both keeps the dual-provider setup.
    [Parameter(Mandatory)]
    [ValidateSet('SqlServer', 'PostgreSql', 'Both')]
    [string]$DatabaseProvider,

    # Deletes the template's .git directory first, then creates a fresh repository with one initial commit.
    [switch]$ResetGitHistory,

    # Skip giving the renamed application its own local ports (used by tests and CI).
    [switch]$SkipLocalPortAllocation
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib/TemplateFiles.ps1')

$repoRoot = Split-Path -Parent $PSScriptRoot
$currentPrefixMatch = [regex]::Match(
    [System.IO.File]::ReadAllText((Join-Path $repoRoot 'AGENTS.md')),
    'Root namespace prefix: `([A-Z][A-Z0-9]*)`')
if (-not $currentPrefixMatch.Success) {
    throw 'AGENTS.md must declare the current root namespace prefix before renaming.'
}
$currentPrefix = $currentPrefixMatch.Groups[1].Value
$currentProductMatch = [regex]::Match(
    [System.IO.File]::ReadAllText((Join-Path $repoRoot 'AGENTS.md')),
    '(?m)^- Product: (.+)$')
if (-not $currentProductMatch.Success) {
    throw 'AGENTS.md must declare the current product before renaming.'
}
$currentDisplayName = $currentProductMatch.Groups[1].Value.Trim()
$currentCompactName = $currentDisplayName -replace '[^A-Za-z0-9]', ''
$currentKebabName = (($currentDisplayName -creplace '([a-z0-9])([A-Z])', '$1-$2') -replace '\s+', '-').ToLowerInvariant()
$currentLowerCompactName = $currentCompactName.ToLowerInvariant()
$displayName = $NewName.Trim()
$compactName = $displayName -replace '[^A-Za-z0-9]', ''
$kebabName = (($displayName -creplace '([a-z0-9])([A-Z])', '$1-$2') -replace '\s+', '-').ToLowerInvariant()
$lowerCompactName = $compactName.ToLowerInvariant()

# --- Git history reset: preflight, then remove .git before anything else changes. ---
$gitDirectory = Join-Path $repoRoot '.git'
if ($ResetGitHistory) {
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        throw '-ResetGitHistory needs git on PATH.'
    }
    if (Test-Path -LiteralPath $gitDirectory) {
        $status = git -C $repoRoot status --porcelain
        if ($LASTEXITCODE -ne 0) { throw 'git status failed; is this a git repository?' }
        if ($status) {
            throw '-ResetGitHistory refuses to run with uncommitted changes. Commit, stash or discard them first.'
        }
        $origin = git -C $repoRoot remote get-url origin 2>$null
        $templateRemotePattern = '(?i)' + [regex]::Escape($currentKebabName) + '|' + [regex]::Escape($currentLowerCompactName)
        if ($LASTEXITCODE -eq 0 -and $origin -and $origin -notmatch $templateRemotePattern) {
            throw "-ResetGitHistory expected 'origin' to be the $currentDisplayName template, but it is '$origin'. Refusing to delete this repository's history."
        }
        if ($PSCmdlet.ShouldProcess($gitDirectory, 'Delete template git history')) {
            Remove-Item -LiteralPath $gitDirectory -Recurse -Force
            Write-Host 'Removed template git history.'
        }
    }
    else {
        Write-Host 'No .git directory found; a fresh repository will be created.'
    }
}

# --- Database provider (runs while project paths still carry the template prefix). ---
& (Join-Path $PSScriptRoot 'select-database-provider.ps1') -Provider $DatabaseProvider -Confirm:$false -WhatIf:$WhatIfPreference

# --- Identity rename. ---
$excludedFiles = @(
    (Join-Path $repoRoot 'ops/local/.env')
)

function New-UserSecretsIdentity {
    if ([Guid].GetMethod('CreateVersion7', [Type[]]@())) {
        return [Guid]::CreateVersion7().ToString()
    }

    # UserSecretsId accepts any unique string; older PowerShell runtimes have no UUIDv7 API.
    $bytes = New-Object byte[] 32
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $generator.Dispose()
    }
}

function Get-ReplacedText([string]$value) {
    # Compact product tokens may occur inside C# identifiers and paths.
    $updated = $value.Replace($currentCompactName, $compactName)
    if ($currentDisplayName -ne $currentCompactName) {
        $updated = $updated.Replace($currentDisplayName, $displayName)
    }
    $updated = $updated.Replace($currentKebabName, $kebabName)
    $updated = $updated.Replace($currentLowerCompactName, $lowerCompactName)
    $updated = $updated.Replace('- Product: ' + $compactName, '- Product: ' + $displayName)

    # Replace only technical prefix forms. A global standalone-BT replacement
    # would corrupt legitimate values such as Bhutan's ISO country code, "BT".
    $technicalPrefixPattern = '(?<![A-Za-z0-9_])' + [regex]::Escape($currentPrefix) + '(?=\.)'
    $updated = $updated -creplace $technicalPrefixPattern, $NamespacePrefix
    $updated = $updated.Replace("${currentPrefix}Api", "${NamespacePrefix}Api")
    $updated = $updated.Replace('Root namespace prefix: `' + $currentPrefix + '`', 'Root namespace prefix: `' + $NamespacePrefix + '`')

    foreach ($oldId in $userSecretsReplacements.Keys) {
        $updated = $updated.Replace($oldId, $userSecretsReplacements[$oldId])
    }
    return $updated
}

if ($displayName -ceq $currentDisplayName -and $NamespacePrefix -ceq $currentPrefix) {
    Write-Host "Template identity is already '$displayName' ($NamespacePrefix)."
}
else {
    # Every cloned host must have an independent user-secrets identity.
    $userSecretsReplacements = @{}
    foreach ($project in Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj') {
        if (Test-TemplateExcludedPath $repoRoot $project.FullName $excludedFiles) { continue }
        [xml]$projectXml = Get-Content -Raw -LiteralPath $project.FullName
        foreach ($identifier in $projectXml.SelectNodes('//UserSecretsId')) {
            if (-not $userSecretsReplacements.ContainsKey($identifier.InnerText)) {
                $userSecretsReplacements[$identifier.InnerText] = New-UserSecretsIdentity
            }
        }
    }

    foreach ($file in Get-TemplateTextFiles $repoRoot $excludedFiles) {
        $content = [System.IO.File]::ReadAllText($file.FullName)
        $updated = Get-ReplacedText $content
        if ($updated -ne $content -and $PSCmdlet.ShouldProcess($file.FullName, 'Replace template identity')) {
            [System.IO.File]::WriteAllText($file.FullName, $updated, [System.Text.UTF8Encoding]::new($false))
        }
    }

    $paths = Get-ChildItem -LiteralPath $repoRoot -Recurse -Force |
        Where-Object { -not (Test-TemplateExcludedPath $repoRoot $_.FullName $excludedFiles) } |
        Sort-Object { $_.FullName.Length } -Descending

    foreach ($path in $paths) {
        $newLeafName = Get-ReplacedText $path.Name
        if ($newLeafName -ne $path.Name -and $PSCmdlet.ShouldProcess($path.FullName, "Rename to '$newLeafName'")) {
            Rename-Item -LiteralPath $path.FullName -NewName $newLeafName
        }
    }
}

# --- Local ports (before the initial commit, so launch settings are committed with them). ---
if (-not $SkipLocalPortAllocation) {
    & (Join-Path $PSScriptRoot 'allocate-local-ports.ps1') -WhatIf:$WhatIfPreference
}

# --- Fresh repository. ---
if ($ResetGitHistory -and -not (Test-Path -LiteralPath $gitDirectory) -and
    $PSCmdlet.ShouldProcess($repoRoot, 'Create a fresh git repository with an initial commit')) {
    git -C $repoRoot init -q -b main
    if ($LASTEXITCODE -ne 0) { throw 'git init failed.' }
    git -C $repoRoot add -A
    git -C $repoRoot commit -q -m "Initial commit from $currentDisplayName template ($DatabaseProvider)"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning 'git commit failed (check git user.name/user.email). The repository is initialised with files staged; commit manually.'
    }
    Write-Host 'Created a fresh git repository on branch main. Connect your new remote with:'
    Write-Host '  git remote add origin <new-remote-url>'
    Write-Host '  git push -u origin main'
}

if ($WhatIfPreference) {
    Write-Host "Previewed '$displayName' ($NamespacePrefix) with database provider '$DatabaseProvider'."
} else {
    Write-Host "Template identity updated to '$displayName' ($NamespacePrefix) with database provider '$DatabaseProvider'."
}
Write-Host 'Review configuration, URLs, tenant seed data, and Azure resource names before the first run.'

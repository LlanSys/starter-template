[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9 ]*$')]
    [string]$NewName,

    [Parameter(Mandatory)]
    [ValidatePattern('^[A-Z][A-Z0-9]*$')]
    [string]$NamespacePrefix
)

$ErrorActionPreference = 'Stop'

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

$excludedDirectoryNames = @('.git', '.vs', '.idea', 'bin', 'obj', 'node_modules', 'Logs', 'artifacts')
$excludedFiles = @(
    (Join-Path $repoRoot 'ops/local/.env')
)
$textExtensions = @(
    '.cs', '.csproj', '.props', '.targets', '.sln', '.slnx', '.json', '.yml', '.yaml',
    '.md', '.html', '.razor', '.css', '.js', '.ts', '.ps1', '.sh', '.xml', '.config',
    '.http', '.xaml', '.manifest', '.Dockerfile', '.editorconfig', '.gitignore', '.gitattributes', '.windsurfrules', '.mdc'
)

function Test-IsExcludedPath([string]$path) {
    if ($excludedFiles -contains $path) {
        return $true
    }

    if ($path.EndsWith('.user', [StringComparison]::OrdinalIgnoreCase) -or
        $path.EndsWith('.suo', [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $relativePath = $path.Substring($repoRoot.Length).TrimStart('\', '/')
    $segments = $relativePath -split '[\\/]'
    return $segments | Where-Object { $excludedDirectoryNames -contains $_ } | Select-Object -First 1
}

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

# Every cloned host must have an independent user-secrets identity.
$userSecretsReplacements = @{}
foreach ($project in Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter '*.csproj') {
    if (Test-IsExcludedPath $project.FullName) { continue }
    [xml]$projectXml = Get-Content -Raw -LiteralPath $project.FullName
    foreach ($identifier in $projectXml.SelectNodes('//UserSecretsId')) {
        if (-not $userSecretsReplacements.ContainsKey($identifier.InnerText)) {
            $userSecretsReplacements[$identifier.InnerText] = New-UserSecretsIdentity
        }
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

$files = Get-ChildItem -LiteralPath $repoRoot -Recurse -File -Force |
    Where-Object {
        -not (Test-IsExcludedPath $_.FullName) -and
        ($textExtensions -contains $_.Extension -or $_.Name -in @('Dockerfile', 'AGENTS.md', 'CLAUDE.md', 'GEMINI.md'))
    }

foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    $updated = Get-ReplacedText $content
    if ($updated -ne $content -and $PSCmdlet.ShouldProcess($file.FullName, 'Replace template identity')) {
        [System.IO.File]::WriteAllText($file.FullName, $updated, [System.Text.UTF8Encoding]::new($false))
    }
}

$paths = Get-ChildItem -LiteralPath $repoRoot -Recurse -Force |
    Where-Object { -not (Test-IsExcludedPath $_.FullName) } |
    Sort-Object { $_.FullName.Length } -Descending

foreach ($path in $paths) {
    $newLeafName = Get-ReplacedText $path.Name
    if ($newLeafName -ne $path.Name -and $PSCmdlet.ShouldProcess($path.FullName, "Rename to '$newLeafName'")) {
        Rename-Item -LiteralPath $path.FullName -NewName $newLeafName
    }
}

Write-Host "Template identity updated to '$displayName' with namespace prefix '$NamespacePrefix'."
Write-Host 'Review configuration, URLs, tenant seed data, and Azure resource names before the first run.'

# Shared helpers for template fork scripts (rename-template.ps1, select-database-provider.ps1).
# Dot-source this file; it defines functions only and has no side effects.

$script:TemplateExcludedDirectoryNames = @(
    '.git', '.vs', '.idea', 'bin', 'obj', 'node_modules', 'Logs', 'artifacts', 'output', 'tmp', 'TestResults'
)

$script:TemplateTextExtensions = @(
    '.cs', '.csproj', '.props', '.targets', '.sln', '.slnx', '.json', '.yml', '.yaml',
    '.md', '.html', '.razor', '.css', '.js', '.ts', '.ps1', '.sh', '.xml', '.config',
    '.http', '.xaml', '.manifest', '.Dockerfile', '.editorconfig', '.gitignore', '.gitattributes', '.windsurfrules', '.mdc'
)

# Provider selection also processes infrastructure and environment templates.
$script:TemplateProviderExtraExtensions = @('.bicep', '.bicepparam', '.example', '.env', '.sql', '.toml')

$script:TemplateTextFileNames = @('Dockerfile', 'AGENTS.md', 'CLAUDE.md', 'GEMINI.md')

# Every file in these directories is text, including extensionless git hook scripts.
$script:TemplateTextDirectoryNames = @('.husky')

function Get-TemplateRelativePath([string]$RepoRoot, [string]$Path) {
    return $Path.Substring($RepoRoot.Length).TrimStart('\', '/').Replace('\', '/')
}

function Test-TemplateExcludedPath([string]$RepoRoot, [string]$Path, [string[]]$ExcludedFiles = @()) {
    if ($ExcludedFiles -contains $Path) {
        return $true
    }

    if ($Path.EndsWith('.user', [StringComparison]::OrdinalIgnoreCase) -or
        $Path.EndsWith('.suo', [StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    $segments = (Get-TemplateRelativePath $RepoRoot $Path) -split '/'
    return [bool]($segments | Where-Object { $script:TemplateExcludedDirectoryNames -contains $_ } | Select-Object -First 1)
}

function Test-TemplateTextFile([System.IO.FileInfo]$File, [switch]$IncludeProviderExtras) {
    if (($script:TemplateTextExtensions -contains $File.Extension) -or ($script:TemplateTextFileNames -contains $File.Name) -or
        ($script:TemplateTextDirectoryNames -contains $File.Directory.Name)) {
        return $true
    }
    return $IncludeProviderExtras -and ($script:TemplateProviderExtraExtensions -contains $File.Extension)
}

function Get-TemplateTextFiles([string]$RepoRoot, [string[]]$ExcludedFiles = @(), [switch]$IncludeProviderExtras) {
    Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Force |
        Where-Object {
            -not (Test-TemplateExcludedPath $RepoRoot $_.FullName $ExcludedFiles) -and
            (Test-TemplateTextFile $_ -IncludeProviderExtras:$IncludeProviderExtras)
        }
}

function ConvertTo-TemplatePathRegex([string]$Glob) {
    # '**' spans directories, '*' stays within one segment, a trailing '/' matches a directory and its contents.
    $pattern = [regex]::Escape($Glob.Replace('\', '/'))
    $pattern = $pattern.Replace('\*\*/', '(?:.*/)?').Replace('\*\*', '.*').Replace('\*', '[^/]*').Replace('\?', '[^/]')
    if ($Glob.EndsWith('/')) {
        return '^' + $pattern + '.*$'
    }
    return '^' + $pattern + '$'
}

# Provider markers: a whole line containing only a comment with <db:Name> or </db:Name>.
# Supported comment forms: //, #, --, REM, and <!-- ... -->.
$script:ProviderMarkerPattern = '^\s*(?://|#|--|REM\b|<!--)\s*<(?<close>/)?db:(?<provider>[A-Za-z]+)>\s*(?:-->)?\s*$'

function Test-ProviderMarkerLine([string]$Line) {
    return $Line -match $script:ProviderMarkerPattern
}

function Remove-ProviderMarkerBlocks {
    <#
    Keeps blocks for providers in $Keep, drops blocks for every other provider, and removes all marker lines.
    Throws on unknown providers, nesting, or unbalanced markers. Line endings are preserved.
    #>
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Content,
        [Parameter(Mandatory)][string[]]$Keep,
        [Parameter(Mandatory)][string[]]$KnownProviders,
        [string]$Source = '<content>'
    )

    $lines = [regex]::Split($Content, '(?<=\n)')
    $builder = [System.Text.StringBuilder]::new($Content.Length)
    $open = $null
    $openLine = 0
    $lineNumber = 0
    foreach ($line in $lines) {
        if ($line.Length -eq 0) { continue }
        $lineNumber++
        $match = [regex]::Match($line.TrimEnd("`r", "`n"), $script:ProviderMarkerPattern)
        if ($match.Success) {
            $provider = $match.Groups['provider'].Value
            if ($KnownProviders -notcontains $provider) {
                throw "${Source}:${lineNumber}: unknown database provider marker '$provider'."
            }
            if ($match.Groups['close'].Success) {
                if ($open -ne $provider) {
                    throw "${Source}:${lineNumber}: closing marker for '$provider' does not match the open block ('$open')."
                }
                $open = $null
            }
            else {
                if ($null -ne $open) {
                    throw "${Source}:${lineNumber}: nested provider marker '$provider' inside '$open' block opened at line $openLine."
                }
                $open = $provider
                $openLine = $lineNumber
            }
            continue
        }

        if ($null -eq $open -or $Keep -contains $open) {
            [void]$builder.Append($line)
        }
    }

    if ($null -ne $open) {
        throw "${Source}:${openLine}: provider marker '$open' is never closed."
    }

    return $builder.ToString()
}

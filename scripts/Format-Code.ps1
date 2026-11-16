[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$WrapLongLines,
    [string[]]$Include = @()
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'StarterTemplate.slnx'
$layoutProject = Join-Path $repoRoot 'tools/ST.SourceFormatting/ST.SourceFormatting.csproj'
$sourceRoots = @('src', 'tests', 'tools')
$diagnostics = @(
    'IDE0011', 'IDE0022', 'IDE2000', 'IDE2003',
    'RCS0010', 'RCS0021', 'RCS0023', 'RCS0030', 'RCS0033',
    'RCS0053', 'RCS0054', 'RCS0060'
)

function Invoke-DotNet {
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($args[0]) failed with exit code $LASTEXITCODE. Review the output above."
    }
}

Push-Location $repoRoot
try {
    $paths = if ($Include.Count) { $Include } else { $sourceRoots }
    $selected = foreach ($path in $paths) {
        $item = Get-Item -LiteralPath $path
        $relative = [IO.Path]::GetRelativePath($repoRoot, $item.FullName).Replace('\', '/')
        $segments = $relative.Split('/')
        if ($segments[0] -notin $sourceRoots) {
            throw "Select a path under src, tests, or tools: $path"
        }
        if (-not $item.PSIsContainer -and $item.Extension -notin @('.cs', '.razor')) {
            throw "Select a C# or Razor source file: $path"
        }

        $relative
    }

    $migrationRoots = Get-ChildItem 'src/Backend/Persistence' -Directory -Recurse -Filter Migrations |
        ForEach-Object { [IO.Path]::GetRelativePath($repoRoot, $_.FullName).Replace('\', '/') }
    $nativeScope = @('--include') + @($selected) + @('--exclude') + @($migrationRoots)
    $layoutScope = @('--include') + @($selected)
    if ($Check) {
        $nativeScope = @('--verify-no-changes') + $nativeScope
    }
    if ($WrapLongLines) {
        $diagnostics += 'RCS0056'
    }

    Invoke-DotNet build $layoutProject --configuration Release --no-restore --verbosity quiet '-p:UseSharedCompilation=false'
    Invoke-DotNet run --project $layoutProject --configuration Release --no-build --no-restore -- --self-test

    if (-not $Check) {
        Invoke-DotNet run --project $layoutProject --configuration Release --no-build --no-restore -- --fix @layoutScope
    }

    Invoke-DotNet format whitespace $solution --no-restore @nativeScope
    Invoke-DotNet format $solution --no-restore --severity info --diagnostics @diagnostics @nativeScope

    if (-not $Check) {
        Invoke-DotNet format whitespace $solution --no-restore @nativeScope
        # Native fixes can expand a statement. Apply logical spacing after that expansion.
        Invoke-DotNet run --project $layoutProject --configuration Release --no-build --no-restore -- --fix @layoutScope
    }

    Invoke-DotNet run --project $layoutProject --configuration Release --no-build --no-restore -- --check @layoutScope
}
finally {
    Pop-Location
}

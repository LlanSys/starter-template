[CmdletBinding()]
param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$Quiet
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
$issues = [Collections.Generic.List[string]]::new()

function Add-Issue([string]$Message) {
    $issues.Add($Message)
}

function Read-PolicyXml([string]$RelativePath) {
    [xml](Get-Content -LiteralPath (Join-Path $RepoRoot $RelativePath) -Raw)
}

$central = Read-PolicyXml 'Directory.Packages.props'
$build = Read-PolicyXml 'Directory.Build.props'
$properties = @{}
foreach ($node in $central.SelectNodes('/Project/PropertyGroup/*')) {
    $properties[$node.Name] = $node.InnerText.Trim()
}

function Resolve-Version([string]$Value) {
    for ($pass = 0; $pass -lt 5; $pass++) {
        foreach ($key in $properties.Keys) {
            $Value = $Value.Replace('$(' + $key + ')', [string]$properties[$key])
        }
    }
    return $Value
}

if ($properties['ManagePackageVersionsCentrally'] -ne 'true') {
    Add-Issue 'Central Package Management must be enabled.'
}
if ($properties['CentralPackageVersionOverrideEnabled'] -ne 'false') {
    Add-Issue 'Per-project central version overrides must be disabled.'
}
if ($properties['CentralPackageTransitivePinningEnabled'] -ne 'false') {
    Add-Issue 'Transitive pinning requires an explicit policy review.'
}
if ($properties['MassTransitVersion'] -notmatch '^8\.\d+\.\d+$') {
    Add-Issue 'MassTransit must retain its approved 8.x shared pin.'
}
if ($properties['RazorStyleVersion'] -notmatch '^2\.\d+\.\d+$') {
    Add-Issue 'RazorStyle must retain its reviewed 2.x formatter contract.'
}

$versions = @{}
foreach ($item in $central.SelectNodes('/Project/ItemGroup/PackageVersion')) {
    $name = $item.GetAttribute('Include')
    $version = Resolve-Version $item.GetAttribute('Version')
    if ([string]::IsNullOrWhiteSpace($name) -or $versions.ContainsKey($name)) {
        Add-Issue "Missing or duplicated central package identity: $name"
        continue
    }
    $versions[$name] = $version
    if ($version -notmatch '^(\d+\.\d+\.\d+(\.\d+)?|\[\d+\.\d+\.\d+\])$') {
        Add-Issue "$name must use an explicit stable version, not a floating/range/prerelease value: $version"
    }
    $platformFamilyPattern = @(
        'Microsoft\.(AspNetCore\.|EntityFrameworkCore($|\.)|Extensions\.)'
        # <db:PostgreSql>
        'Npgsql\.EntityFrameworkCore\.'
        # </db:PostgreSql>
        'System\.Security\.Cryptography\.'
    ) -join '|'
    if ($name -match "^($platformFamilyPattern)" -and $version -notmatch '^10\.') {
        Add-Issue "$name must stay on the approved Microsoft/EF 10.x line: $version"
    }
    if ($name -match '^MassTransit($|\.)' -and $version -ne "[$($properties['MassTransitVersion'])]") {
        Add-Issue "$name must use the exact shared MassTransit pin."
    }
    if ($name -match '^Quartz($|\.)' -and $version -notmatch '^4\.') {
        Add-Issue "$name must retain the reviewed Quartz 4.x schema/serializer contract."
    }
}

foreach ($pattern in @('^FluentValidation($|\.)', '^HotChocolate\.', '^Quartz($|\.)')) {
    $familyVersions = @($versions.Keys | Where-Object { $_ -match $pattern } | ForEach-Object { $versions[$_] } | Sort-Object -Unique)
    if ($familyVersions.Count -gt 1) {
        Add-Issue "Coupled package family $pattern has inconsistent versions: $($familyVersions -join ', ')"
    }
}

$ignoredPath = '[/\\](bin|obj|\.git|node_modules|artifacts|TestResults)[/\\]'
$projectFiles = @(Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Filter '*.csproj' | Where-Object { $_.FullName -notmatch $ignoredPath })
$buildFiles = @($projectFiles) + @(Get-ChildItem -LiteralPath $RepoRoot -Recurse -File | Where-Object { $_.Extension -in @('.props', '.targets') -and $_.FullName -notmatch $ignoredPath })
foreach ($file in $buildFiles) {
    $relative = [IO.Path]::GetRelativePath($RepoRoot, $file.FullName)
    [xml]$document = Get-Content -LiteralPath $file.FullName -Raw
    if ($file.Name -eq 'Directory.Packages.props' -and $file.FullName -ne (Join-Path $RepoRoot 'Directory.Packages.props')) {
        Add-Issue "Nested central manifests require review: $relative"
    }
    foreach ($framework in $document.SelectNodes('//TargetFramework | //TargetFrameworks')) {
        if ($framework.InnerText.Trim() -ne 'net10.0') {
            Add-Issue "$relative must target net10.0: $($framework.InnerText)"
        }
    }
    foreach ($reference in $document.SelectNodes('//PackageReference')) {
        $name = $reference.GetAttribute('Include')
        if ($reference.HasAttribute('Version') -or $reference.HasAttribute('VersionOverride') -or $reference.SelectSingleNode('Version | VersionOverride')) {
            Add-Issue "$relative has a per-project version/override for $name; edit Directory.Packages.props."
        }
        if ($name -and -not $versions.ContainsKey($name)) {
            Add-Issue "$relative references $name without a central PackageVersion."
        }
    }
    foreach ($download in $document.SelectNodes('//PackageDownload')) {
        if ($download.GetAttribute('Include') -ne 'PinguApps.RazorStyle.Cli' -or $download.GetAttribute('Version') -ne '[$(RazorStyleVersion)]') {
            Add-Issue "$relative has an unreviewed package download/version."
        }
    }
    foreach ($setting in $document.SelectNodes('//ManagePackageVersionsCentrally | //CentralPackageVersionOverrideEnabled | //NuGetAudit | //NuGetAuditMode | //NuGetAuditLevel')) {
        $expected = switch ($setting.Name) {
            'ManagePackageVersionsCentrally' { 'true' }
            'CentralPackageVersionOverrideEnabled' { 'false' }
            'NuGetAudit' { 'true' }
            'NuGetAuditMode' { 'all' }
            'NuGetAuditLevel' { 'low' }
        }
        if ($setting.InnerText.Trim() -ne $expected) {
            Add-Issue "$relative weakens $($setting.Name)."
        }
    }
    if ($file.Extension -eq '.csproj') {
        foreach ($assignment in $document.SelectNodes('//WarningsAsErrors')) {
            if (-not $assignment.InnerText.Contains('$(WarningsAsErrors)')) {
                Add-Issue "$relative resets the inherited CI warning errors."
            }
        }
    }
    if ($document.SelectNodes('//NuGetAuditSuppress').Count -gt 0) {
        Add-Issue "$relative suppresses vulnerability advisories; repair the dependency graph."
    }
    foreach ($suppression in $document.SelectNodes('//NoWarn | //*[@NoWarn]/@NoWarn | //WarningsNotAsErrors')) {
        $suppressedText = if ($suppression -is [Xml.XmlAttribute]) { $suppression.Value } else { $suppression.InnerText }
        if ($suppressedText -match 'NU(1510|16\d\d|19\d\d)') {
            Add-Issue "$relative suppresses a guarded NuGet diagnostic."
        }
    }
}

foreach ($setting in @{ NuGetAudit = 'true'; NuGetAuditMode = 'all'; NuGetAuditLevel = 'low' }.GetEnumerator()) {
    $node = $build.SelectSingleNode("/Project/PropertyGroup/$($setting.Key)")
    if ($null -eq $node -or $node.InnerText.Trim() -ne $setting.Value) {
        Add-Issue "Directory.Build.props must set $($setting.Key)=$($setting.Value)."
    }
}
$ciWarnings = ($build.SelectNodes('/Project/PropertyGroup[contains(@Condition,"CI")]/WarningsAsErrors') | ForEach-Object { $_.InnerText }) -join ';'
foreach ($code in @('NU1510', 'NU1605', 'NU1608', 'NU1900', 'NU1901', 'NU1902', 'NU1903', 'NU1904', 'NU1905')) {
    if ($code -notin $ciWarnings.Split(';')) { Add-Issue "CI must treat $code as an error." }
}

$sdk = Get-Content -LiteralPath (Join-Path $RepoRoot 'global.json') -Raw | ConvertFrom-Json
if ($sdk.sdk.version -notmatch '^10\.0\.\d+$' -or $sdk.sdk.rollForward -notin @('disable', 'patch', 'latestPatch', 'feature', 'latestFeature', 'minor', 'latestMinor') -or $sdk.sdk.allowPrerelease -ne $false) {
    Add-Issue 'global.json must select stable .NET 10 without major-version roll-forward.'
}
$tools = Get-Content -LiteralPath (Join-Path $RepoRoot '.config/dotnet-tools.json') -Raw | ConvertFrom-Json
if ($tools.tools.'dotnet-ef'.version -notmatch '^10\.\d+\.\d+$' -or $tools.tools.'dotnet-ef'.rollForward -ne $false) {
    Add-Issue 'dotnet-ef must stay pinned to stable 10.x without tool roll-forward.'
}

$nuget = Read-PolicyXml 'nuget.config'
$sources = @($nuget.SelectNodes('/configuration/packageSources/add'))
if ($sources.Count -ne 1 -or $sources[0].GetAttribute('key') -ne 'nuget.org' -or $sources[0].GetAttribute('value') -ne 'https://api.nuget.org/v3/index.json' -or $null -eq $nuget.SelectSingleNode('/configuration/packageSources/clear') -or $null -eq $nuget.SelectSingleNode('/configuration/packageSourceMapping/clear')) {
    Add-Issue 'NuGet must use the isolated public-only source configuration.'
}

$workflowRoot = Join-Path $RepoRoot '.github/workflows'
if (Test-Path -LiteralPath $workflowRoot) {
    foreach ($workflow in Get-ChildItem -LiteralPath $workflowRoot -File -Filter '*.yml') {
        foreach ($match in [regex]::Matches((Get-Content -LiteralPath $workflow.FullName -Raw), 'dotnet-version:\s*["'']?([^\s"'']+)')) {
            if ($match.Groups[1].Value -notmatch '^10\.0\.(x|\d+)$') { Add-Issue "$($workflow.Name) must install a .NET 10 SDK." }
        }
    }
}
foreach ($dockerfile in @('ops/docker/api.Dockerfile', 'ops/docker/blazor.Dockerfile')) {
    $path = Join-Path $RepoRoot $dockerfile
    if (Test-Path -LiteralPath $path) {
        $content = Get-Content -LiteralPath $path -Raw
        foreach ($match in [regex]::Matches($content, 'FROM\s+mcr\.microsoft\.com/dotnet/(sdk|aspnet):([^\s]+)')) {
            if ($match.Groups[2].Value -notmatch '^10\.0($|[.\-])') { Add-Issue "$dockerfile must use .NET 10 images." }
        }
        if (-not $content.Contains('COPY ["Directory.Packages.props", "."]')) { Add-Issue "$dockerfile must copy the central manifest before restore." }
    }
}

if ($issues.Count -gt 0) {
    throw ("Package policy failed:`n - " + (($issues | Select-Object -First 25) -join "`n - "))
}
if (-not $Quiet) {
    Write-Output "PASS: $($projectFiles.Count) projects, $($versions.Count) central versions; stable .NET 10, coupled-family pins, public feeds and CI audit policy."
}

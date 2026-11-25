[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoots = [System.Collections.Generic.List[string]]::new()

function Assert-Condition([bool]$condition, [string]$message) {
    if (!$condition) { throw $message }
}

function Write-FixtureFile([string]$root, [string]$relative, [string]$content) {
    $path = Join-Path $root $relative
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    [IO.File]::WriteAllText($path, $content, [Text.UTF8Encoding]::new($false))
}

function Read-FixtureFile([string]$root, [string]$relative) {
    return [IO.File]::ReadAllText((Join-Path $root $relative))
}

# A fixture manifest keeps provider selection independent of the real template layout.
$fixtureManifest = @'
{
  "selectedProvider": null,
  "providers": {
    "SqlServer": {
      "paths": [ "src/Db/SqlServer/", "tests/**/*SqlServer*.cs" ],
      "packages": [ "Fixture.SqlServer" ],
      "leftoverPattern": "SqlServer|MsSql",
      "keepReplacements": []
    },
    "PostgreSql": {
      "paths": [ "src/Db/PostgreSql/", "tests/**/*PostgreSql*.cs" ],
      "packages": [ "Fixture.Npgsql" ],
      "leftoverPattern": "PostgreSql|Npgsql",
      "keepReplacements": [
        { "path": "src/appsettings*.json", "pattern": "\"Provider\": \"SqlServer\"", "replacement": "\"Provider\": \"PostgreSql\"" }
      ]
    }
  },
  "templateOnly": [ "scripts/template-only.ps1" ],
  "markerTooling": [ "tests/Markers/" ],
  "leftoverScanExclusions": [ "scripts/**", "**/*.md" ]
}
'@

$sharedSource = @"
namespace SRC.Api;
public static class Db
{
    // <db:SqlServer>
    public const string SqlServerName = "SqlServer";
    // </db:SqlServer>
    // <db:PostgreSql>
    public const string PostgreSqlName = "PostgreSql"; // Npgsql
    // </db:PostgreSql>
    public const string Neutral = "kept";
}
"@

function New-Fixture {
    $root = Join-Path $temporaryRoot ("ExampleSource-rename-" + [Guid]::CreateVersion7().ToString('N'))
    $fixtureRoots.Add($root)
    [IO.Directory]::CreateDirectory((Join-Path $root 'scripts/lib')) | Out-Null
    foreach ($script in @('rename-template.ps1', 'select-database-provider.ps1', 'lib/TemplateFiles.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $script) -Destination (Join-Path $root "scripts/$script")
    }
    Write-FixtureFile $root 'scripts/database-providers.json' $fixtureManifest
    Write-FixtureFile $root 'scripts/template-only.ps1' '# template only'
    Write-FixtureFile $root 'AGENTS.md' ('- Product: ExampleSource' + "`n" + '- Root namespace prefix: `SRC`')
    Write-FixtureFile $root 'ExampleSource.slnx' '<Solution />'
    Write-FixtureFile $root 'src/Backend/Api/SRC.Api/SRC.Api.csproj' '<Project><PropertyGroup><UserSecretsId>api-original</UserSecretsId></PropertyGroup></Project>'
    Write-FixtureFile $root 'src/Frontend/Web/SRC.UI.Blazor/SRC.UI.Blazor.csproj' '<Project><PropertyGroup><UserSecretsId>ui-original</UserSecretsId></PropertyGroup></Project>'
    Write-FixtureFile $root 'src/Backend/Api/SRC.Api/Identity.cs' 'namespace SRC.Api; // api-original ui-original'
    Write-FixtureFile $root 'src/Backend/Api/SRC.Api/Db.cs' $sharedSource
    Write-FixtureFile $root 'src/Db/SqlServer/Context.cs' 'class SqlServerContext {}'
    Write-FixtureFile $root 'src/Db/PostgreSql/Context.cs' 'class PostgreSqlContext {}'
    Write-FixtureFile $root 'tests/SRC.Tests/SqlServerTests.cs' 'class SqlServerTests {}'
    Write-FixtureFile $root 'tests/SRC.Tests/PostgreSqlTests.cs' 'class PostgreSqlTests {}'
    Write-FixtureFile $root 'tests/Markers/MarkerGuardrail.cs' 'class MarkerGuardrail {}'
    Write-FixtureFile $root 'src/appsettings.json' '{ "DatabaseSettings": { "Provider": "SqlServer" } }'
    Write-FixtureFile $root 'Directory.Packages.props' ("<Project>`n  <ItemGroup>`n    <PackageVersion Include=`"Fixture.SqlServer`" Version=`"1.0.0`" />`n    <PackageVersion Include=`"Fixture.Npgsql`" Version=`"1.0.0`" />`n    <PackageVersion Include=`"Fixture.Neutral`" Version=`"1.0.0`" />`n  </ItemGroup>`n</Project>`n")
    Write-FixtureFile $root 'ops/local/.env' 'APP=ExampleSource SRC.Api api-original'
    return $root
}

$previousGitEnvironment = @{}
foreach ($name in 'GIT_AUTHOR_NAME', 'GIT_AUTHOR_EMAIL', 'GIT_COMMITTER_NAME', 'GIT_COMMITTER_EMAIL') {
    $previousGitEnvironment[$name] = [Environment]::GetEnvironmentVariable($name)
}

try {
    # --- Scenario 1: identity rename + PostgreSql selection, preview, rerun, exclusions. ---
    $fixtureRoot = New-Fixture
    $sentinels = @('output/identity.txt', 'tmp/identity.txt', 'tests/Example/TestResults/identity.txt')
    foreach ($file in $sentinels) { Write-FixtureFile $fixtureRoot $file 'ExampleSource SRC.Api api-original' }

    $rename = Join-Path $fixtureRoot 'scripts/rename-template.ps1'
    $originalFile = Join-Path $fixtureRoot 'src/Backend/Api/SRC.Api/SRC.Api.csproj'
    $originalHash = (Get-FileHash -LiteralPath $originalFile).Hash
    & $rename -NewName RenameProbe -NamespacePrefix RP -DatabaseProvider PostgreSql -WhatIf *> (Join-Path $fixtureRoot 'dry-run.log')
    Assert-Condition ((Get-FileHash -LiteralPath $originalFile).Hash -eq $originalHash) 'WhatIf changed the original project.'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $fixtureRoot 'src/Db/SqlServer/Context.cs')) 'WhatIf removed provider files.'

    & $rename -NewName RenameProbe -NamespacePrefix RP -DatabaseProvider PostgreSql -Confirm:$false *> (Join-Path $fixtureRoot 'rename.log')
    $api = Join-Path $fixtureRoot 'src/Backend/Api/RP.Api/RP.Api.csproj'
    $ui = Join-Path $fixtureRoot 'src/Frontend/Web/RP.UI.Blazor/RP.UI.Blazor.csproj'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $fixtureRoot 'RenameProbe.slnx')) 'Solution rename failed.'
    $apiId = ([xml](Get-Content -LiteralPath $api -Raw)).Project.PropertyGroup.UserSecretsId
    $uiId = ([xml](Get-Content -LiteralPath $ui -Raw)).Project.PropertyGroup.UserSecretsId
    Assert-Condition ($apiId -ne $uiId -and $apiId -notin @('api-original', 'ui-original') -and $uiId -notin @('api-original', 'ui-original')) 'Host secret identities were reused.'

    $renamedSource = Read-FixtureFile $fixtureRoot 'src/Backend/Api/RP.Api/Identity.cs'
    Assert-Condition ($renamedSource.Contains('namespace RP.Api;') -and $renamedSource.Contains($apiId) -and $renamedSource.Contains($uiId)) 'Technical namespace or design-time secret references were not updated.'

    # Provider selection results.
    $db = Read-FixtureFile $fixtureRoot 'src/Backend/Api/RP.Api/Db.cs'
    Assert-Condition ($db.Contains('PostgreSqlName') -and $db.Contains('Neutral') -and -not $db.Contains('SqlServerName')) 'Marker blocks were not applied for PostgreSql.'
    Assert-Condition (-not ($db -match '<db:|</db:')) 'Marker lines were left behind.'
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'src/Db/SqlServer'))) 'Removed provider directory still exists.'
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'tests/SRC.Tests/SqlServerTests.cs'))) 'Removed provider test still exists.'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $fixtureRoot 'src/Db/PostgreSql/Context.cs')) 'Kept provider file was removed.'
    $packages = Read-FixtureFile $fixtureRoot 'Directory.Packages.props'
    Assert-Condition (-not $packages.Contains('Fixture.SqlServer') -and $packages.Contains('Fixture.Npgsql') -and $packages.Contains('Fixture.Neutral')) 'Provider packages were not removed correctly.'
    Assert-Condition ((Read-FixtureFile $fixtureRoot 'src/appsettings.json').Contains('"Provider": "PostgreSql"')) 'Kept provider configuration was not applied.'
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'scripts/template-only.ps1'))) 'Template-only tooling was not removed.'
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $fixtureRoot 'tests/Markers'))) 'Marker tooling was not removed.'
    Assert-Condition ((Read-FixtureFile $fixtureRoot 'scripts/database-providers.json').Contains('"selectedProvider": "PostgreSql"')) 'Selected provider was not recorded.'

    $hashes = @{}
    foreach ($file in @($api, $ui, (Join-Path $fixtureRoot 'AGENTS.md'), (Join-Path $fixtureRoot 'src/Backend/Api/RP.Api/Db.cs'))) {
        $hashes[$file] = (Get-FileHash -LiteralPath $file).Hash
    }
    & $rename -NewName RenameProbe -NamespacePrefix RP -DatabaseProvider PostgreSql -Confirm:$false *> (Join-Path $fixtureRoot 'rerun.log')
    foreach ($file in $hashes.Keys) {
        Assert-Condition ((Get-FileHash -LiteralPath $file).Hash -eq $hashes[$file]) 'Identical rename rerun changed source or secret identities.'
    }
    $switchRefused = $false
    try { & $rename -NewName RenameProbe -NamespacePrefix RP -DatabaseProvider SqlServer -Confirm:$false *> $null } catch { $switchRefused = $true }
    Assert-Condition $switchRefused 'Rerun with a different provider was not refused.'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $fixtureRoot 'src/Db/PostgreSql/Context.cs')) 'Provider switch removed the kept provider.'

    foreach ($file in $sentinels) {
        Assert-Condition ((Read-FixtureFile $fixtureRoot $file) -ceq 'ExampleSource SRC.Api api-original') 'Rename modified a generated artifact.'
    }
    Assert-Condition ((Read-FixtureFile $fixtureRoot 'ops/local/.env') -ceq 'APP=ExampleSource SRC.Api api-original') 'Rename modified local environment values.'

    # --- Scenario 2: Both keeps every provider and its markers. ---
    $bothRoot = New-Fixture
    & (Join-Path $bothRoot 'scripts/rename-template.ps1') -NewName BothProbe -NamespacePrefix BP -DatabaseProvider Both -Confirm:$false *> (Join-Path $bothRoot 'rename.log')
    $bothDb = Read-FixtureFile $bothRoot 'src/Backend/Api/BP.Api/Db.cs'
    Assert-Condition ($bothDb.Contains('SqlServerName') -and $bothDb.Contains('PostgreSqlName') -and $bothDb.Contains('<db:SqlServer>')) 'Both removed provider code or markers.'
    Assert-Condition ((Test-Path -LiteralPath (Join-Path $bothRoot 'src/Db/SqlServer/Context.cs')) -and (Test-Path -LiteralPath (Join-Path $bothRoot 'src/Db/PostgreSql/Context.cs'))) 'Both removed provider files.'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $bothRoot 'tests/Markers/MarkerGuardrail.cs')) 'Both removed marker tooling.'
    Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $bothRoot 'scripts/template-only.ps1'))) 'Both kept template-only tooling.'

    # --- Scenario 3: invalid markers and leftovers fail loudly. ---
    $badRoot = New-Fixture
    Write-FixtureFile $badRoot 'src/Broken.cs' "// <db:SqlServer>`nclass Broken {}`n"
    $unbalancedRefused = $false
    try { & (Join-Path $badRoot 'scripts/select-database-provider.ps1') -Provider PostgreSql -Confirm:$false *> $null } catch { $unbalancedRefused = $true }
    Assert-Condition $unbalancedRefused 'Unbalanced provider marker was accepted.'

    $leftoverRoot = New-Fixture
    Write-FixtureFile $leftoverRoot 'src/Leftover.cs' 'class Leftover { string name = "SqlServer"; }'
    $leftoverRefused = $false
    try { & (Join-Path $leftoverRoot 'scripts/select-database-provider.ps1') -Provider PostgreSql -Confirm:$false *> $null } catch { $leftoverRefused = $true }
    Assert-Condition $leftoverRefused 'Unmarked reference to the removed provider was accepted.'

    # --- Scenario 4: git history reset. ---
    if (Get-Command git -ErrorAction SilentlyContinue) {
        $env:GIT_AUTHOR_NAME = 'Rename Probe'; $env:GIT_COMMITTER_NAME = 'Rename Probe'
        $env:GIT_AUTHOR_EMAIL = 'rename-probe@example.invalid'; $env:GIT_COMMITTER_EMAIL = 'rename-probe@example.invalid'

        $gitRoot = New-Fixture
        git -C $gitRoot init -q -b main
        git -C $gitRoot add -A
        git -C $gitRoot commit -q -m 'template history 1'
        git -C $gitRoot commit -q --allow-empty -m 'template history 2'
        git -C $gitRoot remote add origin 'https://example.invalid/acme/example-source.git'
        $gitRename = Join-Path $gitRoot 'scripts/rename-template.ps1'

        Write-FixtureFile $gitRoot 'untracked.txt' 'dirty'
        $dirtyRefused = $false
        try { & $gitRename -NewName GitProbe -NamespacePrefix GP -DatabaseProvider SqlServer -ResetGitHistory -Confirm:$false *> $null } catch { $dirtyRefused = $true }
        Assert-Condition ($dirtyRefused -and (Test-Path -LiteralPath (Join-Path $gitRoot '.git'))) 'Git reset ran on a dirty working tree.'
        Remove-Item -LiteralPath (Join-Path $gitRoot 'untracked.txt')

        git -C $gitRoot remote set-url origin 'https://example.invalid/acme/some-other-product.git'
        $foreignRefused = $false
        try { & $gitRename -NewName GitProbe -NamespacePrefix GP -DatabaseProvider SqlServer -ResetGitHistory -Confirm:$false *> $null } catch { $foreignRefused = $true }
        Assert-Condition ($foreignRefused -and ((git -C $gitRoot rev-list --count HEAD) -eq '2')) 'Git reset ran against a non-template origin.'
        git -C $gitRoot remote set-url origin 'https://example.invalid/acme/example-source.git'

        & $gitRename -NewName GitProbe -NamespacePrefix GP -DatabaseProvider SqlServer -ResetGitHistory -WhatIf *> $null
        Assert-Condition ((git -C $gitRoot rev-list --count HEAD) -eq '2') 'WhatIf reset git history.'

        & $gitRename -NewName GitProbe -NamespacePrefix GP -DatabaseProvider SqlServer -ResetGitHistory -Confirm:$false *> (Join-Path $temporaryRoot ('git-rename-' + [IO.Path]::GetFileName($gitRoot) + '.log'))
        Assert-Condition ((git -C $gitRoot rev-list --count HEAD) -eq '1') 'Fresh repository does not have exactly one commit.'
        Assert-Condition ((git -C $gitRoot branch --show-current) -eq 'main') 'Fresh repository is not on main.'
        Assert-Condition (-not (git -C $gitRoot remote)) 'Fresh repository kept a remote.'
        Assert-Condition (-not (git -C $gitRoot status --porcelain)) 'Fresh repository has uncommitted files.'
        Assert-Condition (Test-Path -LiteralPath (Join-Path $gitRoot 'src/Backend/Api/GP.Api/GP.Api.csproj')) 'Git reset fixture was not renamed.'
    }
    else {
        Write-Warning 'git not found; skipped the -ResetGitHistory scenario.'
    }

    Write-Host 'PASS: rename preview, project paths, host secret identities and references, identical rerun, excluded artifacts, database provider selection (PostgreSql, Both, invalid markers, leftovers, provider switch refusal), and git history reset.'
}
finally {
    foreach ($name in $previousGitEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previousGitEnvironment[$name])
    }
    $expectedParent = $temporaryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    foreach ($fixtureRoot in $fixtureRoots) {
        $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
        if (!$resolvedFixture.StartsWith($expectedParent, [StringComparison]::OrdinalIgnoreCase) -or
            [IO.Path]::GetFileName($resolvedFixture) -notlike 'ExampleSource-rename-*') {
            throw 'Unexpected rename fixture cleanup path.'
        }
        if (Test-Path -LiteralPath $resolvedFixture) {
            Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
        }
    }
}

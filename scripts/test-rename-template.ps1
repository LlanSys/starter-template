[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $temporaryRoot ("ExampleSource-rename-" + [Guid]::CreateVersion7().ToString('N'))

function Assert-Condition([bool]$condition, [string]$message) {
    if (!$condition) { throw $message }
}

function Write-FixtureFile([string]$relative, [string]$content) {
    $path = Join-Path $fixtureRoot $relative
    [IO.Directory]::CreateDirectory((Split-Path -Parent $path)) | Out-Null
    [IO.File]::WriteAllText($path, $content, [Text.UTF8Encoding]::new($false))
}

try {
    [IO.Directory]::CreateDirectory((Join-Path $fixtureRoot 'scripts')) | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'rename-template.ps1') -Destination (Join-Path $fixtureRoot 'scripts/rename-template.ps1')
    Write-FixtureFile 'AGENTS.md' ('- Product: ExampleSource' + "`n" + '- Root namespace prefix: `SRC`')
    Write-FixtureFile 'ExampleSource.slnx' '<Solution />'
    Write-FixtureFile 'src/Backend/Api/SRC.Api/SRC.Api.csproj' '<Project><PropertyGroup><UserSecretsId>api-original</UserSecretsId></PropertyGroup></Project>'
    Write-FixtureFile 'src/Frontend/Web/SRC.UI.Blazor/SRC.UI.Blazor.csproj' '<Project><PropertyGroup><UserSecretsId>ui-original</UserSecretsId></PropertyGroup></Project>'
    Write-FixtureFile 'src/Backend/Api/SRC.Api/Identity.cs' 'namespace SRC.Api; // api-original ui-original'
    Write-FixtureFile 'ops/local/.env' 'APP=ExampleSource SRC.Api api-original'
    $sentinels = @('output/identity.txt', 'tmp/identity.txt', 'tests/Example/TestResults/identity.txt')
    foreach ($file in $sentinels) { Write-FixtureFile $file 'ExampleSource SRC.Api api-original' }

    $rename = Join-Path $fixtureRoot 'scripts/rename-template.ps1'
    $originalFile = Join-Path $fixtureRoot 'src/Backend/Api/SRC.Api/SRC.Api.csproj'
    $originalHash = (Get-FileHash -LiteralPath $originalFile).Hash
    & $rename -NewName RenameProbe -NamespacePrefix RP -WhatIf *> (Join-Path $fixtureRoot 'dry-run.log')
    Assert-Condition ((Get-FileHash -LiteralPath $originalFile).Hash -eq $originalHash) 'WhatIf changed the original project.'

    & $rename -NewName RenameProbe -NamespacePrefix RP -Confirm:$false *> (Join-Path $fixtureRoot 'rename.log')
    $api = Join-Path $fixtureRoot 'src/Backend/Api/RP.Api/RP.Api.csproj'
    $ui = Join-Path $fixtureRoot 'src/Frontend/Web/RP.UI.Blazor/RP.UI.Blazor.csproj'
    Assert-Condition (Test-Path -LiteralPath (Join-Path $fixtureRoot 'RenameProbe.slnx')) 'Solution rename failed.'
    $apiId = ([xml](Get-Content -LiteralPath $api -Raw)).Project.PropertyGroup.UserSecretsId
    $uiId = ([xml](Get-Content -LiteralPath $ui -Raw)).Project.PropertyGroup.UserSecretsId
    Assert-Condition ($apiId -ne $uiId -and $apiId -notin @('api-original', 'ui-original') -and $uiId -notin @('api-original', 'ui-original')) 'Host secret identities were reused.'

    $renamedSource = [IO.File]::ReadAllText((Join-Path $fixtureRoot 'src/Backend/Api/RP.Api/Identity.cs'))
    Assert-Condition ($renamedSource.Contains('namespace RP.Api;') -and $renamedSource.Contains($apiId) -and $renamedSource.Contains($uiId)) 'Technical namespace or design-time secret references were not updated.'
    $hashes = @{}
    foreach ($file in @($api, $ui, (Join-Path $fixtureRoot 'AGENTS.md'))) {
        $hashes[$file] = (Get-FileHash -LiteralPath $file).Hash
    }
    & $rename -NewName RenameProbe -NamespacePrefix RP -Confirm:$false *> (Join-Path $fixtureRoot 'rerun.log')
    foreach ($file in $hashes.Keys) {
        Assert-Condition ((Get-FileHash -LiteralPath $file).Hash -eq $hashes[$file]) 'Identical rename rerun changed source or secret identities.'
    }
    foreach ($file in $sentinels) {
        Assert-Condition ([IO.File]::ReadAllText((Join-Path $fixtureRoot $file)) -ceq 'ExampleSource SRC.Api api-original') 'Rename modified a generated artifact.'
    }
    Assert-Condition ([IO.File]::ReadAllText((Join-Path $fixtureRoot 'ops/local/.env')) -ceq 'APP=ExampleSource SRC.Api api-original') 'Rename modified local environment values.'
    Write-Host 'PASS: rename preview, project paths, host secret identities and references, identical rerun, and excluded artifacts.'
}
finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $expectedParent = $temporaryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (!$resolvedFixture.StartsWith($expectedParent, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedFixture) -notlike 'ExampleSource-rename-*') {
        throw 'Unexpected rename fixture cleanup path.'
    }
    if (Test-Path -LiteralPath $resolvedFixture) {
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}

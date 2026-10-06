[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot 'src/Backend/Api/ST.Api/ST.Api.csproj'
$webLaunchSettings = Join-Path $repoRoot 'src/Frontend/Web/ST.UI.Blazor/Properties/launchSettings.json'

function Add-FlattenedSettings($Node, [string]$Prefix, [hashtable]$Destination) {
    foreach ($property in $Node.PSObject.Properties) {
        $key = if ($Prefix) { "$($Prefix):$($property.Name)" } else { $property.Name }
        if ($property.Value -is [System.Management.Automation.PSCustomObject]) {
            Add-FlattenedSettings $property.Value $key $Destination
        } else {
            $Destination[$key] = $property.Value
        }
    }
}

[xml]$project = Get-Content -Raw -LiteralPath $apiProject
$userSecretsId = $project.SelectSingleNode('//UserSecretsId').InnerText
if ([string]::IsNullOrWhiteSpace($userSecretsId)) {
    throw 'The API project must have its own UserSecretsId before payment setup.'
}
$secretsRoot = if ($env:APPDATA) { Join-Path $env:APPDATA 'Microsoft/UserSecrets' } else { Join-Path $HOME '.microsoft/usersecrets' }
$secretsFile = Join-Path (Join-Path $secretsRoot $userSecretsId) 'secrets.json'
$existing = @{}
if (Test-Path -LiteralPath $secretsFile) {
    Add-FlattenedSettings (Get-Content -Raw -LiteralPath $secretsFile | ConvertFrom-Json) '' $existing
}

$configuration = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'src/Backend/Api/ST.Api/appsettings.Development.json') | ConvertFrom-Json
$defaults = @{}
Add-FlattenedSettings $configuration.Payments 'Payments' $defaults
$launch = Get-Content -Raw -LiteralPath $webLaunchSettings | ConvertFrom-Json
$webOrigin = ($launch.profiles.https.applicationUrl -split ';' | Where-Object { $_ -like 'https://*' } | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($webOrigin)) {
    throw 'The Web HTTPS launch profile must declare an origin for Stripe return URLs.'
}
$defaults['Payments:Stripe:SuccessUrl'] = "$($webOrigin.TrimEnd('/'))/features/payments"
$defaults['Payments:Stripe:CancelUrl'] = "$($webOrigin.TrimEnd('/'))/features/payments"

$missing = @{}
foreach ($key in $defaults.Keys) {
    if (-not $existing.ContainsKey($key)) { $missing[$key] = $defaults[$key] }
}
if ($missing.Count -eq 0) {
    Write-Host 'Payment settings already exist; all existing values were preserved.'
    return
}
# Import only missing keys, including intentionally blank provider credentials.
$missing | ConvertTo-Json -Depth 8 | & dotnet user-secrets set --project $apiProject
if ($LASTEXITCODE -ne 0) { throw "Payment settings import failed with exit code $LASTEXITCODE." }
Write-Host "Added $($missing.Count) missing payment settings. Existing values were preserved."
Write-Host 'Fill the provider credentials in API Manage User Secrets; blank credentials keep providers unconfigured.'
Write-Host 'M-Pesa CallbackUrlBase is the public API origin, without a callback route.'

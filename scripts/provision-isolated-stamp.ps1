[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$tenantId = [Guid]::Parse($env:PROVISIONING_TENANT_ID)
$stampId = [Guid]::Parse($env:PROVISIONING_STAMP_ID)
[void][Guid]::Parse($env:PROVISIONING_OPERATION_ID)
$provider = $env:PROVISIONING_DATABASE_PROVIDER
if ($provider -notin @('SqlServer', 'PostgreSql') -or $provider -ne $env:PROVISIONING_CONTROLPLANE_PROVIDER) {
    throw 'Choose the database provider configured for this control plane.'
}
foreach ($name in @('PROVISIONING_RESOURCE_GROUP','PROVISIONING_REGION','PROVISIONING_SQL_PASSWORD',
    'PROVISIONING_CONTROLPLANE_CONNECTION','PROVISIONING_TRUSTED_PROXY_CIDR','RUNNER_TEMP','GITHUB_OUTPUT')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) { throw "Configure $name." }
}
$network = [System.Net.IPNetwork]::Parse($env:PROVISIONING_TRUSTED_PROXY_CIDR)
if ($network.PrefixLength -eq 0) { throw 'Configure a bounded trusted ingress network.' }
$prefix = 'st' + $stampId.ToString('N').Substring(0, 16)
$temporary = Join-Path $env:RUNNER_TEMP ('stamp-' + [Guid]::CreateVersion7().ToString('N') + '.json')
function Invoke-Azure {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)
    $output = & az @Arguments --only-show-errors 2>$null
    if ($LASTEXITCODE -ne 0) { throw ('Azure operation failed for ' + $Arguments[0] + ' ' + $Arguments[1] + '.') }
    return ($output -join [Environment]::NewLine)
}
try {
    [void](Invoke-Azure @('group','create','--name',$env:PROVISIONING_RESOURCE_GROUP,'--location',$env:PROVISIONING_REGION,'--output','none'))
    $parameters = @{
        location = @{ value = $env:PROVISIONING_REGION }
        resourcePrefix = @{ value = $prefix }
        databaseProvider = @{ value = $provider }
        sqlAdministratorLogin = @{ value = 'stampadmin' }
        sqlAdministratorLoginPassword = @{ value = $env:PROVISIONING_SQL_PASSWORD }
    }
    [IO.File]::WriteAllText($temporary, (ConvertTo-Json -InputObject @{ parameters = $parameters } -Depth 5))
    if (-not $IsWindows) { & chmod 600 $temporary }
    $result = Invoke-Azure @('deployment','group','create','--resource-group',$env:PROVISIONING_RESOURCE_GROUP,
        '--template-file','ops/azure/stamp-isolated/main.bicep','--parameters',('@' + $temporary),
        '--query','properties.outputs','--output','json') | ConvertFrom-Json
    $vault = ([Uri]$result.keyVaultUri.value).Host.Split('.')[0]
    [IO.File]::WriteAllText($temporary, $env:PROVISIONING_CONTROLPLANE_CONNECTION)
    [void](Invoke-Azure @('keyvault','secret','set','--vault-name',$vault,
        '--name','ConnectionStrings--ControlPlaneConnection','--file',$temporary,'--output','none'))

    $lookup = & az keyvault secret show --vault-name $vault --name JwtSettings--SecretKey --query id --output tsv --only-show-errors 2>&1
    if ($LASTEXITCODE -ne 0) {
        if (($lookup -join '') -notmatch 'SecretNotFound') {
            throw 'Cannot verify the existing stamp signing key.'
        }
        $signingKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
        [IO.File]::WriteAllText($temporary,$signingKey)
        [void](Invoke-Azure @('keyvault','secret','set','--vault-name',$vault,'--name','JwtSettings--SecretKey','--file',$temporary,'--output','none'))
    }
    $connection = Invoke-Azure @('keyvault','secret','show','--vault-name',$vault,
        '--name','ConnectionStrings--DefaultConnection','--query','value','--output','tsv')
    Write-Host ('::add-mask::' + $connection)
    $env:ConnectionStrings__IamConnection = $connection
    $env:ConnectionStrings__SharedConnection = $connection
    $env:ConnectionStrings__ControlPlaneConnection = $env:PROVISIONING_CONTROLPLANE_CONNECTION
    $env:DOTNET_ENVIRONMENT = 'Development'
    foreach ($context in @("Iam${provider}DBContext", "Shared${provider}DBContext")) {
        & dotnet ef database update --project src/Backend/Persistence/ST.Persistence/ST.Persistence.csproj --startup-project src/Backend/Api/ST.Api/ST.Api.csproj --context $context --configuration Release --no-build
        if ($LASTEXITCODE -ne 0) { throw 'Destination schema migration failed.' }
    }
    $apiHost = $result.apiHost.value
    $uiHost = $result.uiHost.value
    $realm = 'urn:startertemplate:stamp:' + $stampId.ToString('N')
    $apiEnvironment = @(
        "Tenant__DeploymentStampId=$stampId", "Tenant__DefaultTenantId=$tenantId",
        "Tenant__CentralApiHosts__0=$apiHost", "JwtSettings__Issuer=$realm", "JwtSettings__Audience=$realm",
        "EmailSettings__ClientBaseUrl=https://$uiHost", "ExternalSignIn__PublicApiOrigin=https://$apiHost",
        "ExternalSignIn__ClientOrigins__0=https://$uiHost",
        "TrustedForwardedHeaders__Networks__0=$($env:PROVISIONING_TRUSTED_PROXY_CIDR)",
        'DevelopmentSeed__Enabled=false','ControlPlane__Provisioning__Enabled=false','EmailSettings__Provider=AzureCommunication'
    )
    [void](Invoke-Azure (@('containerapp','update','--resource-group',$env:PROVISIONING_RESOURCE_GROUP,
        '--name',$result.apiAppNameOutput.value,'--set-env-vars') + $apiEnvironment + @('--output','none')))
    [void](Invoke-Azure @('containerapp','update','--resource-group',$env:PROVISIONING_RESOURCE_GROUP,
        '--name',$result.uiAppNameOutput.value,'--set-env-vars',"BackendApi__BaseUrl=https://$apiHost/","BackendApi__PublicBaseUrl=https://$apiHost",
        '--output','none'))
    @(
        'api_app_name=' + $result.apiAppNameOutput.value
        'ui_app_name=' + $result.uiAppNameOutput.value
        'api_host=' + $apiHost
        'ui_host=' + $uiHost
        'key_vault_name=' + $vault
    ) | Add-Content -LiteralPath $env:GITHUB_OUTPUT
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
    $env:ConnectionStrings__IamConnection = $null
    $env:ConnectionStrings__SharedConnection = $null
    $env:ConnectionStrings__ControlPlaneConnection = $null
}

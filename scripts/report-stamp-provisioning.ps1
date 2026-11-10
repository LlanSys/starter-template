[CmdletBinding()]
param([switch]$Succeeded)

$ErrorActionPreference = 'Stop'
$tenantId = [Guid]::Parse($env:PROVISIONING_TENANT_ID)
$operationId = [Guid]::Parse($env:PROVISIONING_OPERATION_ID)
$origin = [Uri]$env:PROVISIONING_CALLBACK_ORIGIN
if ($origin.Scheme -ne 'https' -or $origin.AbsolutePath -ne '/' -or $origin.UserInfo -or $origin.Query -or $origin.Fragment) {
    throw 'Configure an HTTPS control-plane callback origin.'
}
if ([string]::IsNullOrWhiteSpace($env:PROVISIONING_CALLBACK_AUDIENCE)) {
    throw 'Configure the provisioning callback audience.'
}

$audience = [Uri]::EscapeDataString($env:PROVISIONING_CALLBACK_AUDIENCE)
$identity = Invoke-RestMethod -Uri ($env:ACTIONS_ID_TOKEN_REQUEST_URL + '&audience=' + $audience) -Headers @{
    Authorization = 'Bearer ' + $env:ACTIONS_ID_TOKEN_REQUEST_TOKEN
}
if ([string]::IsNullOrWhiteSpace($identity.value)) { throw 'GitHub did not issue a provisioning identity.' }
Write-Host ('::add-mask::' + $identity.value)

$payload = @{
    operationId = $operationId.ToString()
    succeeded = [bool]$Succeeded
    databaseConnectionString = ''
    applicationInsightsKey = ''
    databaseProvider = $env:PROVISIONING_DATABASE_PROVIDER
}
if ($Succeeded) {
    $connection = & az keyvault secret show --vault-name $env:PROVISIONING_KEY_VAULT_NAME --name ConnectionStrings--DefaultConnection --query value --output tsv --only-show-errors
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($connection)) {
        throw 'Cannot retrieve the provisioned workspace database.'
    }
    Write-Host ('::add-mask::' + $connection)
    $payload.databaseConnectionString = $connection
    $payload.clientOrigin = 'https://' + $env:PROVISIONING_UI_HOST
}
$endpoint = [Uri]::new($origin, "/api/v1/control-plane/provisioning/$tenantId/attempts/$operationId")
$body = $payload | ConvertTo-Json -Compress
try {
    $result = Invoke-RestMethod -Uri $endpoint -Method Post -ContentType 'application/json' -Body $body -Headers @{
        Authorization = 'Bearer ' + $identity.value
    } -MaximumRetryCount 3 -RetryIntervalSec 3 -TimeoutSec 90
    if (-not $result.isSuccess) { throw 'The control plane rejected this provisioning result.' }
} catch {
    throw 'The provisioning result could not be accepted. Inspect the current attempt and control-plane diagnostics.'
}
Write-Host 'The control plane accepted the provisioning result.'

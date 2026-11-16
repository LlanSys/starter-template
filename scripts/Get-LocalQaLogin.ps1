[CmdletBinding()]
param(
    [string]$Workspace = 'default',
    [string]$Username,
    [switch]$List,
    [switch]$EnrollmentKey
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'This local QA handoff uses Windows user-bound encryption.' }
$path = Join-Path $env:LOCALAPPDATA 'StarterTemplate/OperatorQA/credentials.dpapi'
if (-not (Test-Path -LiteralPath $path)) { throw 'No local QA account handoff exists for this Windows user.' }
$entropy = [Text.Encoding]::UTF8.GetBytes('StarterTemplate.OperatorQA.v1')
$bytes = [Security.Cryptography.ProtectedData]::Unprotect(
    [IO.File]::ReadAllBytes($path), $entropy, [Security.Cryptography.DataProtectionScope]::CurrentUser)
try {
    $accounts = [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json -AsHashtable
}
finally { [Array]::Clear($bytes) }
$platformId = '0194f700-0000-7000-8000-000000000001'
if ($List) {
    $accounts.Values | Sort-Object tenant, username | ForEach-Object {
        [pscustomobject]@{
            Workspace = $(if ($_.tenant -eq $platformId) { 'default' } else { $_.workspace })
            Username = $_.username
            Profile = $_.profile
            MfaEnrolled = [bool]$_.seed
        }
    }
    return
}
if ([string]::IsNullOrWhiteSpace($Username) -or -not $Username.StartsWith('qa.', [StringComparison]::Ordinal)) {
    throw 'Choose a generated qa. username, or use -List.'
}
$matches = @($accounts.Values | Where-Object {
    $_.username -eq $Username -and (($_.tenant -eq $platformId -and $Workspace -eq 'default') -or $_.workspace -eq $Workspace -or $_.tenant -eq $Workspace)
})
if ($matches.Count -ne 1) { throw 'Select one existing QA account by its exact workspace and username.' }
$account = $matches[0]
$code = $null
if ($account.seed) {
    $alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
    $buffer = 0; $bits = 0
    $key = [Collections.Generic.List[byte]]::new()
    foreach ($character in $account.seed.ToUpperInvariant().TrimEnd('=').ToCharArray()) {
        $value = $alphabet.IndexOf($character)
        if ($value -lt 0) { throw 'Invalid local QA authenticator material.' }
        $buffer = ($buffer -shl 5) -bor $value
        $bits += 5
        if ($bits -ge 8) {
            $bits -= 8
            $key.Add([byte](($buffer -shr $bits) -band 255))
            $buffer = $buffer -band ((1 -shl $bits) - 1)
        }
    }
    $counter = [BitConverter]::GetBytes([long][Math]::Floor([DateTimeOffset]::UtcNow.ToUnixTimeSeconds() / 30))
    if ([BitConverter]::IsLittleEndian) { [Array]::Reverse($counter) }
    $hmac = [Security.Cryptography.HMACSHA1]::new($key.ToArray())
    try { $hash = $hmac.ComputeHash($counter) }
    finally { $hmac.Dispose() }
    $offset = $hash[19] -band 15
    $number = (([long]($hash[$offset] -band 127)) -shl 24) -bor (([long]$hash[$offset + 1]) -shl 16) -bor (([long]$hash[$offset + 2]) -shl 8) -bor ([long]$hash[$offset + 3])
    $code = ($number % 1000000).ToString('D6')
}
$result = [ordered]@{ SignIn = 'http://localhost:8181/iam/sign-in'; Workspace = $Workspace; Username = $account.username; Password = $account.password; AuthenticatorCode = $code }
if ($EnrollmentKey) { $result.EnrollmentKey = $account.seed }
[pscustomobject]$result

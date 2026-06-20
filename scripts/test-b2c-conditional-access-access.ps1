# Verifies management app Graph permissions for Conditional Access (diagnostic).
#
# Usage:
#   .\scripts\test-b2c-conditional-access-access.ps1 -Environment dev

param(
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment = 'dev',

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

Write-Host '=== Conditional Access Graph access diagnostic ===' -ForegroundColor Cyan
Write-Host "Tenant ID:           $TenantId"
Write-Host "Management client ID: $ManagementClientId"
Write-Host ''

$token = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret
$parts = $token.Split('.')
if ($parts.Count -ge 2) {
    $payload = $parts[1]
    $mod = $payload.Length % 4
    if ($mod -gt 0) { $payload += ('=' * (4 - $mod)) }
    $json = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload.Replace('-', '+').Replace('_', '/')))
    $claims = $json | ConvertFrom-Json
    Write-Host "Token app id (appid): $($claims.appid)"
    Write-Host "Token tenant (tid):   $($claims.tid)"
    if ($claims.tid -ne $TenantId) {
        Write-Host 'WARNING: Token tenant does not match B2C_TENANT_ID.' -ForegroundColor Yellow
    }
    if ($claims.appid -ne $ManagementClientId.Trim()) {
        Write-Host 'WARNING: Token app id does not match B2C_MANAGEMENT_CLIENT_ID.' -ForegroundColor Yellow
    }
    Write-Host ''
}

$tests = @(
    @{ Name = 'Email OTP policy'; Path = 'policies/authenticationMethodsPolicy/authenticationMethodConfigurations/email' }
    @{ Name = 'Security Defaults'; Path = 'policies/identitySecurityDefaultsEnforcementPolicy' }
    @{ Name = 'List CA policies'; Path = 'identity/conditionalAccess/policies' }
    @{ Name = 'List applications'; Path = 'applications?$top=1' }
)

foreach ($test in $tests) {
    Write-Host "Testing: $($test.Name) ..."
    try {
        $result = Invoke-B2cGraphApi -AccessToken $token -Method Get -Path $test.Path -ApiVersion v1.0
        if ($test.Name -eq 'Security Defaults') {
            $state = if ($result.isEnabled) { 'enabled (must disable for CA)' } else { 'disabled' }
            Write-Host "  OK ($state)" -ForegroundColor $(if ($result.isEnabled) { 'Yellow' } else { 'Green' })
        }
        else {
            Write-Host '  OK' -ForegroundColor Green
        }
    }
    catch {
        Write-Host "  FAILED" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message.Split([Environment]::NewLine)[0])"
    }
}

Write-Host ''
Write-Host 'If List CA policies FAILED, add Policy.Read.All + Policy.ReadWrite.ConditionalAccess + Application.Read.All in vitalnexusexternal and grant admin consent.' -ForegroundColor Yellow

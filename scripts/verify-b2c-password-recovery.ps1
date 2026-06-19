# Verifies Entra External ID self-service password recovery configuration.
#
# Usage:
#   .\scripts\verify-b2c-password-recovery.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$recoveryConfigPath = Join-Path $repoRoot 'infra/identity/password-recovery/environments.json'
$userFlowConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
$brandingConfigPath = Join-Path $repoRoot 'infra/identity/branding/environments.json'

$recoveryConfig = (Get-Content $recoveryConfigPath -Raw | ConvertFrom-Json).$Environment
$userFlowConfig = (Get-Content $userFlowConfigPath -Raw | ConvertFrom-Json).$Environment
$brandingConfig = (Get-Content $brandingConfigPath -Raw | ConvertFrom-Json).$Environment

$tenantKind = if ($userFlowConfig.tenantKind) { $userFlowConfig.tenantKind } else { 'b2c' }
$locale = if ($brandingConfig.locale) { $brandingConfig.locale } else { 'en' }
$showForgotPasswordLink = $recoveryConfig.showForgotPasswordLink -ne $false

Write-Host '=== Entra External ID password recovery verification ===' -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "Tenant:      $($userFlowConfig.tenantDomainPrefix).onmicrosoft.com"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

$emailOtp = Get-EmailOtpAuthenticationMethodConfiguration -AccessToken $accessToken
Write-Host "Email OTP state:                    $($emailOtp.state)"
Write-Host "allowExternalIdToUseEmailOtp:       $($emailOtp.allowExternalIdToUseEmailOtp)"

if ($recoveryConfig.emailOtpMethod -eq 'enabled') {
    if ($emailOtp.state -ne 'enabled') {
        throw "Expected Email OTP state 'enabled' but found '$($emailOtp.state)'."
    }
    if ($emailOtp.allowExternalIdToUseEmailOtp -notin @('enabled', 'default')) {
        throw "Expected allowExternalIdToUseEmailOtp 'enabled' or 'default' but found '$($emailOtp.allowExternalIdToUseEmailOtp)'."
    }
}

$localization = Get-OrganizationBrandingLocalization -AccessToken $accessToken -OrganizationId $TenantId -Locale $locale
$brandingSource = if ($null -ne $localization) { $localization } else { Get-OrganizationBranding -AccessToken $accessToken -OrganizationId $TenantId }

$visibility = $brandingSource.loginPageTextVisibilitySettings
if ($null -eq $visibility) {
    if ($showForgotPasswordLink) {
        Write-Host 'Note: loginPageTextVisibilitySettings not set; confirm Forgot password link in portal.' -ForegroundColor Yellow
    }
}
else {
    Write-Host "hideForgotMyPassword:                 $($visibility.hideForgotMyPassword)"
    if ($showForgotPasswordLink -and $visibility.hideForgotMyPassword -eq $true) {
        throw 'Forgot password link is hidden (hideForgotMyPassword=true).'
    }
}

if ($tenantKind -eq 'b2c') {
    $passwordResetFlowId = $recoveryConfig.passwordResetUserFlowId
    if (-not (Test-B2cUserFlowExists -AccessToken $accessToken -UserFlowId $passwordResetFlowId)) {
        throw "Legacy B2C password reset user flow not found: $passwordResetFlowId"
    }

    Write-Host "Password reset user flow:             $passwordResetFlowId"
}
else {
    Write-Host 'CIAM: built-in SSPR on sign-up/sign-in flow (no separate reset flow to verify).'
}

Write-Host ''
Write-Host 'Password recovery verification passed.' -ForegroundColor Green

# Configures self-service password recovery (SSPR) for Entra External ID customer accounts.
#
# Usage:
#   $env:B2C_TENANT_ID = '<tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   .\scripts\configure-b2c-password-recovery.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,

    [switch]$SkipEmailOtpConfiguration
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$recoveryConfigPath = Join-Path $repoRoot 'infra/identity/password-recovery/environments.json'
$userFlowConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
$brandingConfigPath = Join-Path $repoRoot 'infra/identity/branding/environments.json'
$b2cFlowTemplatePath = Join-Path $repoRoot 'infra/identity/password-recovery/password-reset.b2c.flow.json'

$recoveryConfig = (Get-Content $recoveryConfigPath -Raw | ConvertFrom-Json).$Environment
$userFlowConfig = (Get-Content $userFlowConfigPath -Raw | ConvertFrom-Json).$Environment
$brandingConfig = (Get-Content $brandingConfigPath -Raw | ConvertFrom-Json).$Environment

$tenantKind = if ($userFlowConfig.tenantKind) { $userFlowConfig.tenantKind } else { 'b2c' }
$tenantDomain = "$($userFlowConfig.tenantDomainPrefix).onmicrosoft.com"
$locale = if ($brandingConfig.locale) { $brandingConfig.locale } else { 'en' }
$showForgotPasswordLink = $recoveryConfig.showForgotPasswordLink -ne $false
$customForgotText = if ($recoveryConfig.customForgotMyPasswordText) { [string]$recoveryConfig.customForgotMyPasswordText } else { '' }

Write-Host '=== Configure Entra External ID password recovery ===' -ForegroundColor Cyan
Write-Host "Environment:  $Environment"
Write-Host "Tenant kind:  $tenantKind"
Write-Host "Tenant:       $tenantDomain"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

if (-not $SkipEmailOtpConfiguration -and $recoveryConfig.emailOtpMethod -eq 'enabled') {
    Enable-ExternalIdEmailOtpForPasswordReset -AccessToken $accessToken
}
elseif ($SkipEmailOtpConfiguration) {
    Write-Host 'Skipping Email OTP configuration (-SkipEmailOtpConfiguration).'
}
else {
    Write-Host 'Email OTP method left unchanged (emailOtpMethod not set to enabled in config).'
}

Set-PasswordResetLinkBranding `
    -AccessToken $accessToken `
    -OrganizationId $TenantId `
    -Locale $locale `
    -ShowForgotPasswordLink $showForgotPasswordLink `
    -CustomForgotMyPasswordText $customForgotText

if ($tenantKind -eq 'b2c') {
    $flowTemplate = Get-Content $b2cFlowTemplatePath -Raw | ConvertFrom-Json
    $flowTemplate.id = $recoveryConfig.passwordResetUserFlowShortId
    New-B2cPasswordResetUserFlowIfMissing -AccessToken $accessToken -UserFlowId $recoveryConfig.passwordResetUserFlowId -FlowTemplate $flowTemplate
}
else {
    Write-Host 'CIAM tenant: password recovery uses built-in SSPR on the sign-up/sign-in user flow (no separate reset flow).'
    Write-Host "Ensure F3.T1.4 flow uses Email + password: $($userFlowConfig.userFlowDisplayName)"
}

Write-Host ''
Write-Host 'Password recovery configuration complete.' -ForegroundColor Green
Write-Host "Verify with:"
Write-Host "  .\scripts\verify-b2c-password-recovery.ps1 -Environment $Environment"
Write-Host ''
Write-Host 'Manual test: sign-in page -> Forgot password? -> Email OTP -> new password.' -ForegroundColor Yellow

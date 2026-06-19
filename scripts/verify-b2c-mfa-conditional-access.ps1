# Verifies Entra External ID MFA Conditional Access configuration.
#
# Usage:
#   .\scripts\verify-b2c-mfa-conditional-access.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,
    [string]$SpaClientId = $env:B2C_SPA_CLIENT_ID,

    [switch]$AllowDisabledPolicy
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$mfaConfigPath = Join-Path $repoRoot 'infra/identity/mfa/environments.json'
$userFlowConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
$mfaConfig = (Get-Content $mfaConfigPath -Raw | ConvertFrom-Json).$Environment
$userFlowConfig = (Get-Content $userFlowConfigPath -Raw | ConvertFrom-Json).$Environment
$policyDisplayName = $mfaConfig.conditionalAccessPolicyDisplayName
$expectedState = if ($mfaConfig.policyState) { $mfaConfig.policyState } else { 'enabled' }
$targetSpa = $mfaConfig.targetSpaApplication -ne $false

Write-Host '=== Entra External ID MFA verification ===' -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "Tenant:      $($userFlowConfig.tenantDomainPrefix).onmicrosoft.com"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

if ($targetSpa) {
    $SpaClientId = Resolve-B2cSpaClientId `
        -AccessToken $accessToken `
        -Environment $Environment `
        -SpaClientId $SpaClientId `
        -ManagementClientId $ManagementClientId `
        -RepoRoot $repoRoot
}

$emailOtp = Get-EmailOtpAuthenticationMethodConfiguration -AccessToken $accessToken
Write-Host "Email OTP state:                    $($emailOtp.state)"
Write-Host "allowExternalIdToUseEmailOtp:       $($emailOtp.allowExternalIdToUseEmailOtp)"

if ($mfaConfig.emailOtpMethod -eq 'enabled') {
    if ($emailOtp.state -ne 'enabled') {
        throw "Expected Email OTP state 'enabled' but found '$($emailOtp.state)'."
    }
}

$securityDefaults = Get-IdentitySecurityDefaultsPolicy -AccessToken $accessToken
Write-Host "Security Defaults:                  $(if ($securityDefaults.isEnabled) { 'enabled' } else { 'disabled' })"
if ($securityDefaults.isEnabled -eq $true) {
    throw 'Security Defaults must be disabled before Conditional Access MFA can apply. Re-run configure script.'
}

$policy = Get-ConditionalAccessPolicyByDisplayName -AccessToken $accessToken -DisplayName $policyDisplayName
if ($null -eq $policy) {
    throw "Conditional Access policy not found: $policyDisplayName"
}

Write-Host "Conditional Access policy:          $($policy.displayName)"
Write-Host "Policy state:                       $($policy.state)"
Write-Host "Policy ID:                          $($policy.id)"

if (-not $AllowDisabledPolicy -and $policy.state -ne $expectedState) {
    throw "Expected policy state '$expectedState' but found '$($policy.state)'."
}

$grantControls = $policy.grantControls.builtInControls
Write-Host "Grant builtInControls:              $($grantControls -join ', ')"

if ($grantControls -notcontains 'mfa') {
    throw 'Conditional Access policy does not require MFA.'
}

if ($targetSpa) {
    if ([string]::IsNullOrWhiteSpace($SpaClientId)) {
        throw 'B2C_SPA_CLIENT_ID is required for verification when targetSpaApplication is true.'
    }

    $includedApps = @($policy.conditions.applications.includeApplications)
    Write-Host "Target applications:                $($includedApps -join ', ')"

    if ($includedApps -notcontains $SpaClientId.Trim()) {
        throw "Conditional Access policy does not target SPA app '$SpaClientId'."
    }
}

Write-Host ''
Write-Host 'MFA Conditional Access verification passed.' -ForegroundColor Green

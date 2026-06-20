# Configures customer MFA via Entra Conditional Access for External ID tenants.
#
# Usage:
#   $env:B2C_TENANT_ID = '<tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   $env:B2C_SPA_CLIENT_ID = '<spa-client-id>'
#   .\scripts\configure-b2c-mfa-conditional-access.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,
    [string]$SpaClientId = $env:B2C_SPA_CLIENT_ID,

    [switch]$ReportOnly,
    [switch]$SkipEmailOtpConfiguration
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

$tenantKind = if ($userFlowConfig.tenantKind) { $userFlowConfig.tenantKind } else { 'b2c' }
$tenantDomain = "$($userFlowConfig.tenantDomainPrefix).onmicrosoft.com"
$policyDisplayName = $mfaConfig.conditionalAccessPolicyDisplayName
$policyState = if ($ReportOnly) { 'disabled' } elseif ($mfaConfig.policyState) { $mfaConfig.policyState } else { 'enabled' }
$targetSpa = $mfaConfig.targetSpaApplication -ne $false
$includeAllUsers = $mfaConfig.includeAllUsers -ne $false
$builtInControls = @($mfaConfig.grantBuiltInControls)

Write-Host '=== Configure Entra External ID MFA (Conditional Access) ===' -ForegroundColor Cyan
Write-Host "Environment:  $Environment"
Write-Host "Tenant kind:  $tenantKind"
Write-Host "Tenant:       $tenantDomain"
Write-Host "CA policy:    $policyDisplayName"
Write-Host "Policy state: $policyState"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

if ($targetSpa) {
    $SpaClientId = Resolve-B2cSpaClientId `
        -AccessToken $accessToken `
        -Environment $Environment `
        -SpaClientId $SpaClientId `
        -ManagementClientId $ManagementClientId `
        -RepoRoot $repoRoot
    Write-Host ''
}

if (-not $SkipEmailOtpConfiguration -and $mfaConfig.emailOtpMethod -eq 'enabled') {
    Enable-ExternalIdEmailOtpAuthenticationMethod -AccessToken $accessToken -Purpose 'MFA second factor'
}
elseif ($SkipEmailOtpConfiguration) {
    Write-Host 'Skipping Email OTP configuration (-SkipEmailOtpConfiguration).'
}

Disable-IdentitySecurityDefaultsIfEnabled -AccessToken $accessToken
Write-Host ''

$policyId = Set-ConditionalAccessMfaPolicyForApplication `
    -AccessToken $accessToken `
    -DisplayName $policyDisplayName `
    -SpaClientId $SpaClientId `
    -BuiltInControls $builtInControls `
    -PolicyState $policyState `
    -IncludeAllUsers $includeAllUsers `
    -TargetSpaApplication $targetSpa

if ($tenantKind -eq 'b2c') {
    Write-Host ''
    Write-Host 'Legacy B2C: also set user flow MFA enforcement to Conditional in the portal if MFA is not triggered.' -ForegroundColor Yellow
    Write-Host "  User flows -> $($userFlowConfig.userFlowId) -> Multifactor authentication -> Conditional + Email OTP"
}

Write-Host ''
Write-Host 'MFA Conditional Access configuration complete.' -ForegroundColor Green
Write-Host "Policy ID: $policyId"
Write-Host "Verify with:"
Write-Host "  .\scripts\verify-b2c-mfa-conditional-access.ps1 -Environment $Environment"

if ($ReportOnly) {
    Write-Host ''
    Write-Host 'Report-only: policy was saved as disabled. Enable in portal or re-run without -ReportOnly.' -ForegroundColor Yellow
}

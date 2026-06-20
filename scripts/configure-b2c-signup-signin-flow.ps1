# Creates or updates the VitalNexus customer sign-up/sign-in user flow in Entra External ID via Microsoft Graph.
# Supports CIAM (Entra External ID) and legacy B2C tenant kinds.
#
# Usage:
#   $env:B2C_TENANT_ID = '<tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   $env:B2C_SPA_CLIENT_ID = '<spa-client-id>'   # optional; links SPA app to CIAM flow
#   .\scripts\configure-b2c-signup-signin-flow.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,
    [string]$SpaClientId = $env:B2C_SPA_CLIENT_ID,

    [switch]$SkipBrandingPortalReminder
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$envConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
$flowTemplatePath = Join-Path $repoRoot 'infra/identity/user-flows/signup-signin.flow.json'
$allEnvConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
$envConfig = $allEnvConfig.$Environment
$flowTemplate = Get-Content $flowTemplatePath -Raw | ConvertFrom-Json

$tenantKind = if ($envConfig.tenantKind) { $envConfig.tenantKind } else { 'b2c' }
$userFlowId = $envConfig.userFlowId
$userFlowDisplayName = if ($envConfig.userFlowDisplayName) { $envConfig.userFlowDisplayName } else { $envConfig.displayName }
$tenantDomain = "$($envConfig.tenantDomainPrefix).onmicrosoft.com"

Write-Host '=== Configure Entra External ID sign-up/sign-in user flow ===' -ForegroundColor Cyan
Write-Host "Environment:  $Environment"
Write-Host "Tenant kind:  $tenantKind"
Write-Host "Tenant:       $tenantDomain"
Write-Host "User flow:    $userFlowDisplayName"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

if ($tenantKind -eq 'ciam') {
    $flow = New-CiamUserFlowIfMissing -AccessToken $accessToken -DisplayName $userFlowDisplayName -SpaClientId $SpaClientId
    Write-Host ''
    Write-Host "CIAM user flow ID: $($flow.id)"
}
else {
    New-B2cUserFlowIfMissing -AccessToken $accessToken -UserFlowId $userFlowId -FlowTemplate $flowTemplate
    Write-Host 'Enabling English language customization...'
    Enable-B2cUserFlowEnglish -AccessToken $accessToken -UserFlowId $userFlowId
}

Write-Host ''
Write-Host 'User flow configuration complete.' -ForegroundColor Green
Write-Host "Verify with:"
Write-Host "  .\scripts\verify-b2c-user-flow.ps1 -Environment $Environment"

if (-not $SkipBrandingPortalReminder) {
    Write-Host ''
    Write-Host 'Branded sign-in pages (F3.T1.5):' -ForegroundColor Yellow
    Write-Host "  .\scripts\configure-b2c-auth-branding.ps1 -Environment $Environment"
    Write-Host '  See infra/identity/branding/README.md for logo assets and portal fallback.'
}

# Verifies that the VitalNexus sign-up/sign-in user flow is configured.
# Supports CIAM (Entra External ID) and legacy B2C tenant kinds.
#
# Usage:
#   .\scripts\verify-b2c-user-flow.ps1 -Environment dev
#   .\scripts\verify-b2c-user-flow.ps1 -TenantDomainPrefix vitalnexusexternal -TenantKind ciam -TenantId <guid>

param(
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment = '',

    [string]$TenantDomainPrefix = '',

    [ValidateSet('ciam', 'b2c', '')]
    [string]$TenantKind = '',

    [string]$TenantId = $env:B2C_TENANT_ID,

    [string]$UserFlowId = '',

    [string]$UserFlowDisplayName = '',

    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,
    [string]$SpaClientId = $env:B2C_SPA_CLIENT_ID
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'

if (-not [string]::IsNullOrWhiteSpace($Environment)) {
    $envConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
    $allEnvConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
    $envConfig = $allEnvConfig.$Environment
    $TenantDomainPrefix = $envConfig.tenantDomainPrefix
    if ([string]::IsNullOrWhiteSpace($TenantKind)) {
        $TenantKind = if ($envConfig.tenantKind) { $envConfig.tenantKind } else { 'b2c' }
    }
    if ([string]::IsNullOrWhiteSpace($UserFlowId)) {
        $UserFlowId = $envConfig.userFlowId
    }
    if ([string]::IsNullOrWhiteSpace($UserFlowDisplayName)) {
        $UserFlowDisplayName = if ($envConfig.userFlowDisplayName) { $envConfig.userFlowDisplayName } else { $envConfig.displayName }
    }
}

if ([string]::IsNullOrWhiteSpace($TenantDomainPrefix)) {
    throw 'Provide -Environment or -TenantDomainPrefix.'
}

if ([string]::IsNullOrWhiteSpace($TenantKind)) {
    $TenantKind = 'b2c'
}

$tenantDomain = "$TenantDomainPrefix.onmicrosoft.com"

Write-Host '=== Entra External ID user flow verification ===' -ForegroundColor Cyan
Write-Host "Tenant kind: $TenantKind"
Write-Host "Tenant:      $tenantDomain"
Write-Host ''

if ($TenantKind -eq 'ciam') {
    if ([string]::IsNullOrWhiteSpace($TenantId) -or
        [string]::IsNullOrWhiteSpace($ManagementClientId) -or
        [string]::IsNullOrWhiteSpace($ManagementClientSecret)) {
        throw 'CIAM verification requires B2C_TENANT_ID, B2C_MANAGEMENT_CLIENT_ID, and B2C_MANAGEMENT_CLIENT_SECRET.'
    }

    . (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')
    $accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret
    $flow = Get-CiamUserFlowByDisplayName -AccessToken $accessToken -DisplayName $UserFlowDisplayName

    if ($null -eq $flow) {
        throw "CIAM user flow not found: $UserFlowDisplayName"
    }

    Write-Host "User flow ID:   $($flow.id)"
    Write-Host "Display name:   $($flow.displayName)"

    $SpaClientId = Resolve-B2cSpaClientId `
        -AccessToken $accessToken `
        -Environment $Environment `
        -SpaClientId $SpaClientId `
        -ManagementClientId $ManagementClientId `
        -RepoRoot $repoRoot
    $linkedAppIds = Get-CiamUserFlowLinkedAppIds -AccessToken $accessToken -FlowId $flow.id
    Write-Host "Linked SPA app:   $($linkedAppIds -join ', ')"
    if ($linkedAppIds -notcontains $SpaClientId.Trim()) {
        throw "CIAM user flow '$UserFlowDisplayName' is not linked to SPA app '$SpaClientId'. Run .\scripts\configure-b2c-signup-signin-flow.ps1 -Environment $Environment"
    }

    $metadataUrl = "https://$TenantDomainPrefix.ciamlogin.com/$TenantId/v2.0/.well-known/openid-configuration"
    Write-Host "Metadata URL:   $metadataUrl"
    Write-Host ''

    $metadata = Invoke-RestMethod -Method Get -Uri $metadataUrl -TimeoutSec 30
    if ([string]::IsNullOrWhiteSpace($metadata.issuer)) {
        throw 'OpenID metadata did not return an issuer.'
    }

    Write-Host "issuer:                 $($metadata.issuer)"
    Write-Host "authorization_endpoint:  $($metadata.authorization_endpoint)"
    Write-Host "token_endpoint:         $($metadata.token_endpoint)"
    Write-Host "jwks_uri:               $($metadata.jwks_uri)"
}
else {
    if ([string]::IsNullOrWhiteSpace($UserFlowId)) {
        $UserFlowId = 'B2C_1_VitalNexusSignUpSignIn'
    }

    $metadataUrl = "https://$TenantDomainPrefix.b2clogin.com/$tenantDomain/$UserFlowId/v2.0/.well-known/openid-configuration"
    Write-Host "User flow:      $UserFlowId"
    Write-Host "Metadata URL:   $metadataUrl"
    Write-Host ''

    $metadata = Invoke-RestMethod -Method Get -Uri $metadataUrl -TimeoutSec 30
    if ([string]::IsNullOrWhiteSpace($metadata.issuer)) {
        throw 'OpenID metadata did not return an issuer.'
    }

    if ($metadata.issuer -notmatch [regex]::Escape($UserFlowId)) {
        throw "Issuer does not reference user flow '$UserFlowId'. Issuer: $($metadata.issuer)"
    }

    Write-Host "issuer:                 $($metadata.issuer)"
    Write-Host "authorization_endpoint:  $($metadata.authorization_endpoint)"
    Write-Host "token_endpoint:         $($metadata.token_endpoint)"
    Write-Host "jwks_uri:               $($metadata.jwks_uri)"
}

Write-Host ''
Write-Host 'User flow verification passed.' -ForegroundColor Green

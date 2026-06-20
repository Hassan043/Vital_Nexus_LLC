# Verifies that a Microsoft Entra / Azure AD B2C tenant responds to OpenID Connect discovery.
# Usage:
#   .\scripts\verify-b2c-tenant.ps1 -TenantDomain vitalnexusdev.onmicrosoft.com
#   .\scripts\verify-b2c-tenant.ps1 -TenantId 00000000-0000-0000-0000-000000000000

param(
    [string]$TenantDomain = "",
    [string]$TenantId = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($TenantDomain) -and [string]::IsNullOrWhiteSpace($TenantId)) {
    throw "Provide -TenantDomain or -TenantId."
}

function Test-OpenIdConfiguration {
    param([string]$MetadataUrl)

    Write-Host "Checking $MetadataUrl"
    $metadata = Invoke-RestMethod -Method Get -Uri $MetadataUrl -TimeoutSec 30

    if ([string]::IsNullOrWhiteSpace($metadata.issuer)) {
        throw "OpenID metadata did not return an issuer."
    }

    Write-Host "  issuer: $($metadata.issuer)"
    return $metadata
}

Write-Host "=== B2C tenant verification ==="

if (-not [string]::IsNullOrWhiteSpace($TenantId)) {
    $url = "https://login.microsoftonline.com/$TenantId/v2.0/.well-known/openid-configuration"
    Test-OpenIdConfiguration -MetadataUrl $url | Out-Null
}

if (-not [string]::IsNullOrWhiteSpace($TenantDomain)) {
    $url = "https://login.microsoftonline.com/$TenantDomain/v2.0/.well-known/openid-configuration"
    Test-OpenIdConfiguration -MetadataUrl $url | Out-Null

    $prefix = $TenantDomain.Replace('.onmicrosoft.com', '')
    $b2cLoginUrl = "https://$prefix.b2clogin.com/$TenantDomain/v2.0/.well-known/openid-configuration"
    try {
        Test-OpenIdConfiguration -MetadataUrl $b2cLoginUrl | Out-Null
    }
    catch {
        Write-Host "  Note: b2clogin metadata not yet available (expected before user flows are configured)."
    }
}

Write-Host ""
Write-Host "Tenant verification passed." -ForegroundColor Green

# Verifies VitalNexus company branding on Entra External ID sign-in pages.
#
# Usage:
#   .\scripts\verify-b2c-auth-branding.ps1 -Environment dev

param(
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment = '',

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,

    [string]$ExpectedSignInTitle = ''
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

if ([string]::IsNullOrWhiteSpace($Environment)) {
    throw 'Provide -Environment.'
}

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$brandingConfigPath = Join-Path $repoRoot 'infra/identity/branding/environments.json'
$userFlowConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
$brandingConfig = (Get-Content $brandingConfigPath -Raw | ConvertFrom-Json).$Environment
$userFlowConfig = (Get-Content $userFlowConfigPath -Raw | ConvertFrom-Json).$Environment
$locale = if ($brandingConfig.locale) { $brandingConfig.locale } else { 'en' }

if ([string]::IsNullOrWhiteSpace($ExpectedSignInTitle) -and $brandingConfig.contentCustomization.SignIn_Title) {
    $ExpectedSignInTitle = [string]$brandingConfig.contentCustomization.SignIn_Title
}

Write-Host '=== Entra External ID authentication branding verification ===' -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "Tenant:      $($userFlowConfig.tenantDomainPrefix).onmicrosoft.com"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret
$branding = Get-OrganizationBranding -AccessToken $accessToken -OrganizationId $TenantId
$localization = Get-OrganizationBrandingLocalization -AccessToken $accessToken -OrganizationId $TenantId -Locale $locale

Write-Host "Default backgroundColor:       $($branding.backgroundColor)"
Write-Host "Default headerBackgroundColor: $($branding.headerBackgroundColor)"
Write-Host "Default signInPageText:        $($branding.signInPageText)"
Write-Host "Default usernameHintText:      $($branding.usernameHintText)"

if ($brandingConfig.backgroundColor -and $branding.backgroundColor -ne $brandingConfig.backgroundColor) {
    throw "Expected backgroundColor '$($brandingConfig.backgroundColor)' but found '$($branding.backgroundColor)'."
}

$brandingSource = if ($null -ne $localization) { $localization } else { $branding }
$attributeCollection = @()
if ($brandingSource.contentCustomization -and $brandingSource.contentCustomization.attributeCollection) {
    $attributeCollection = @($brandingSource.contentCustomization.attributeCollection)
}

$signInTitle = ($attributeCollection | Where-Object { $_.key -eq 'SignIn_Title' } | Select-Object -First 1).value
Write-Host "SignIn_Title:                  $signInTitle"

if (-not [string]::IsNullOrWhiteSpace($ExpectedSignInTitle) -and $signInTitle -ne $ExpectedSignInTitle) {
    throw "Expected SignIn_Title '$ExpectedSignInTitle' but found '$signInTitle'."
}

if ($brandingConfig.assets.bannerLogo) {
    $bannerUrl = if ($localization) { $localization.bannerLogoRelativeUrl } else { $branding.bannerLogoRelativeUrl }
    if ([string]::IsNullOrWhiteSpace($bannerUrl)) {
        Write-Host 'Note: bannerLogo not uploaded yet (optional asset).' -ForegroundColor Yellow
    }
    else {
        Write-Host "bannerLogoRelativeUrl:         $bannerUrl"
    }
}

Write-Host ''
Write-Host 'Authentication branding verification passed.' -ForegroundColor Green

# Applies VitalNexus company branding to Entra External ID sign-in/sign-up pages via Microsoft Graph.
#
# Usage:
#   $env:B2C_TENANT_ID = '<tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   .\scripts\configure-b2c-auth-branding.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,

    [switch]$ApplyLegacyUserFlowStrings,
    [switch]$SkipAssetUpload
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$brandingConfigPath = Join-Path $repoRoot 'infra/identity/branding/environments.json'
$userFlowConfigPath = Join-Path $repoRoot 'infra/identity/user-flows/environments.json'
$legacyStringsPath = Join-Path $repoRoot 'infra/identity/branding/legacy-b2c-user-flow-strings.en.json'
$brandingRoot = Join-Path $repoRoot 'infra/identity/branding'

$brandingConfig = (Get-Content $brandingConfigPath -Raw | ConvertFrom-Json).$Environment
$userFlowConfig = (Get-Content $userFlowConfigPath -Raw | ConvertFrom-Json).$Environment
$tenantKind = if ($userFlowConfig.tenantKind) { $userFlowConfig.tenantKind } else { 'b2c' }
$tenantDomain = "$($userFlowConfig.tenantDomainPrefix).onmicrosoft.com"
$locale = if ($brandingConfig.locale) { $brandingConfig.locale } else { 'en' }

Write-Host '=== Configure Entra External ID authentication branding ===' -ForegroundColor Cyan
Write-Host "Environment:  $Environment"
Write-Host "Tenant kind:  $tenantKind"
Write-Host "Tenant:       $tenantDomain"
Write-Host "Locale:       $locale"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

Write-Host 'Checking branding API access...'
$brandingInitialized = Test-B2cBrandingApiAccess -AccessToken $accessToken -OrganizationId $TenantId
if ($brandingInitialized) {
    Write-Host 'Company branding already initialized.'
}
else {
    Write-Host 'Company branding not initialized yet (expected on new tenants); will bootstrap via English localization.' -ForegroundColor Yellow
}
Write-Host ''

$contentCustomization = @{}
if ($brandingConfig.contentCustomization) {
    $brandingConfig.contentCustomization.PSObject.Properties | ForEach-Object {
        $contentCustomization[$_.Name] = [string]$_.Value
    }
}

$brandingBody = @{}
if ($brandingConfig.backgroundColor) { $brandingBody.backgroundColor = [string]$brandingConfig.backgroundColor }
if ($brandingConfig.headerBackgroundColor) { $brandingBody.headerBackgroundColor = [string]$brandingConfig.headerBackgroundColor }
if ($brandingConfig.signInPageText) { $brandingBody.signInPageText = [string]$brandingConfig.signInPageText }
if ($brandingConfig.usernameHintText) { $brandingBody.usernameHintText = [string]$brandingConfig.usernameHintText }

$contentCustomizationBody = New-ContentCustomizationBody -ContentCustomization $contentCustomization
if ($null -ne $contentCustomizationBody) {
    $brandingBody.contentCustomization = $contentCustomizationBody
}

Write-Host "Updating company branding localization ($locale)..."
Set-OrganizationBrandingLocalizationStrings -AccessToken $accessToken -OrganizationId $TenantId -Locale $locale -BrandingProperties $brandingBody

if ($tenantKind -ne 'ciam') {
    Write-Host 'Updating default company branding...'
    Set-OrganizationBrandingStrings -AccessToken $accessToken -OrganizationId $TenantId -BrandingProperties $brandingBody
}
else {
    Write-Host 'CIAM tenant: applied branding via English localization (default branding patch skipped).'
}

if (-not $SkipAssetUpload -and $brandingConfig.assets) {
    $streamProperties = @(
        @{ Name = 'bannerLogo'; Path = $brandingConfig.assets.bannerLogo }
        @{ Name = 'squareLogo'; Path = $brandingConfig.assets.squareLogo }
        @{ Name = 'favicon'; Path = $brandingConfig.assets.favicon }
    )

    foreach ($asset in $streamProperties) {
        if ([string]::IsNullOrWhiteSpace($asset.Path)) {
            continue
        }

        $assetPath = Join-Path $brandingRoot $asset.Path
        if (Test-Path -LiteralPath $assetPath) {
            Write-Host "Uploading $($asset.Name): $assetPath"
            Set-OrganizationBrandingStream -AccessToken $accessToken -OrganizationId $TenantId -PropertyName $asset.Name -FilePath $assetPath -Locale $locale
        }
        else {
            Write-Host "Skipping $($asset.Name) (file not found: $assetPath)"
        }
    }
}

if ($tenantKind -eq 'b2c' -and $ApplyLegacyUserFlowStrings) {
    $userFlowId = $userFlowConfig.userFlowId
    if (Test-B2cUserFlowExists -AccessToken $accessToken -UserFlowId $userFlowId) {
        Write-Host "Applying legacy B2C user-flow language overrides for $userFlowId..."
        Enable-B2cUserFlowEnglish -AccessToken $accessToken -UserFlowId $userFlowId

        $stringsTemplate = Get-Content $legacyStringsPath -Raw | ConvertFrom-Json
        $overridePages = Get-B2cUserFlowOverridePages -AccessToken $accessToken -UserFlowId $userFlowId -LanguageId $locale
        $targetPages = @($overridePages | Where-Object { $_.id -eq 'Unified' })
        if ($targetPages.Count -eq 0) {
            $targetPages = @($overridePages | Select-Object -First 1)
        }

        foreach ($page in $targetPages) {
            Write-Host "Updating overrides page: $($page.id)"
            Set-B2cUserFlowLanguageOverridePageContent `
                -AccessToken $accessToken `
                -UserFlowId $userFlowId `
                -LanguageId $locale `
                -PageId $page.id `
                -LocalizedStrings $stringsTemplate.localizedStrings
        }
    }
    else {
        Write-Host "Legacy B2C user flow '$userFlowId' not found; skipping user-flow string overrides."
    }
}
else {
    Write-Host 'Company branding applied via organization/branding (use -ApplyLegacyUserFlowStrings for legacy B2C user-flow page overrides).'
}

Write-Host ''
Write-Host 'Authentication branding configuration complete.' -ForegroundColor Green
Write-Host "Verify with:"
Write-Host "  .\scripts\verify-b2c-auth-branding.ps1 -Environment $Environment"

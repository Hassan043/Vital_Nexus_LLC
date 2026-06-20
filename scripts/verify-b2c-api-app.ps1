# Verifies the VitalNexus backend API app registration and exposed OAuth2 scopes in Entra External ID.
#
# Usage:
#   $env:B2C_TENANT_ID = '<b2c-tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   .\scripts\verify-b2c-api-app.ps1 -Environment dev

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

$envConfigPath = Join-Path $repoRoot 'infra/identity/api-app/environments.json'
$allEnvConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
$envConfig = $allEnvConfig.$Environment
$displayName = $envConfig.displayName
$tenantDomain = "$($envConfig.tenantDomainPrefix).onmicrosoft.com"
$applicationIdUri = "https://$tenantDomain/$($envConfig.scopeNamespace)"

Write-Host '=== Verify Entra External ID API app ===' -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "App name:    $displayName"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

$filter = [uri]::EscapeDataString("displayName eq '$displayName'")
$existingApps = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications?`$filter=$filter" -ApiVersion v1.0

$appSummary = $existingApps.value | Where-Object { $_.appId -ne $ManagementClientId.Trim() } | Select-Object -First 1

if ($null -eq $appSummary) {
    throw "API app registration not found: $displayName. Run register-b2c-api-app.ps1 first."
}

$app = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications/$($appSummary.id)" -ApiVersion v1.0

if ($null -eq $app.api -or $null -eq $app.api.oauth2PermissionScopes) {
    throw "API app '$displayName' does not expose OAuth2 scopes."
}

$registeredIdentifierUris = @($app.identifierUris)
$registeredScopes = @($app.api.oauth2PermissionScopes)

Write-Host "Client ID:            $($app.appId)"
Write-Host "Identifier URIs:      $($registeredIdentifierUris -join ', ')"
Write-Host "Registered scopes:    $($registeredScopes.value -join ', ')"

if ($registeredIdentifierUris -notcontains $applicationIdUri) {
    throw "Missing Application ID URI: $applicationIdUri"
}

foreach ($expectedScope in $envConfig.scopes) {
    $match = $registeredScopes | Where-Object { $_.value -eq $expectedScope.value -and $_.isEnabled } | Select-Object -First 1
    if ($null -eq $match) {
        throw "Missing or disabled scope: $($expectedScope.value)"
    }
}

Write-Host ''
Write-Host "Primary scope URI:      $applicationIdUri/$($envConfig.scopes[0].value)"
Write-Host ''
Write-Host 'API app registration verification passed.' -ForegroundColor Green

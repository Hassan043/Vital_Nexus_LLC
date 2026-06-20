# Verifies the VitalNexus frontend SPA app registration exists in Entra External ID.
#
# Usage:
#   $env:B2C_TENANT_ID = '<b2c-tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   .\scripts\verify-b2c-spa-app.ps1 -Environment dev

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

$envConfigPath = Join-Path $repoRoot 'infra/identity/spa-app/environments.json'
$allEnvConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
$envConfig = $allEnvConfig.$Environment
$displayName = $envConfig.displayName

Write-Host '=== Verify Entra External ID SPA app ===' -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "App name:    $displayName"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

$filter = [uri]::EscapeDataString("displayName eq '$displayName'")
$existingApps = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications?`$filter=$filter" -ApiVersion v1.0

$app = $existingApps.value | Where-Object { $_.appId -ne $ManagementClientId.Trim() } | Select-Object -First 1

if ($null -eq $app) {
    if ($existingApps.value.Count -gt 0) {
        throw "Only the management app matches display name '$displayName'. Rename the management app and register a separate SPA app."
    }

    throw "SPA app registration not found: $displayName. Run register-b2c-spa-app.ps1 first."
}
$registeredUris = @($app.spa.redirectUris)
$expectedUris = @($envConfig.redirectUris)

Write-Host "Client ID:       $($app.appId)"
Write-Host "Registered URIs: $($registeredUris -join ', ')"

foreach ($expectedUri in $expectedUris) {
    if ($registeredUris -notcontains $expectedUri) {
        throw "Missing required redirect URI: $expectedUri"
    }
}

Write-Host ''
Write-Host 'SPA app registration verification passed.' -ForegroundColor Green

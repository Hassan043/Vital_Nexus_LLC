# Grants the VitalNexus SPA delegated permission to the VitalNexus API scope (F3.T1.8).
#
# Usage:
#   .\scripts\grant-b2c-spa-api-permission.ps1 -Environment dev

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,
    [string]$SpaClientId = $env:B2C_SPA_CLIENT_ID
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$spaConfig = (Get-Content (Join-Path $repoRoot 'infra/identity/spa-app/environments.json') -Raw | ConvertFrom-Json).$Environment
$apiConfig = (Get-Content (Join-Path $repoRoot 'infra/identity/api-app/environments.json') -Raw | ConvertFrom-Json).$Environment
$scopeValue = $apiConfig.scopes[0].value

Write-Host '=== Grant SPA delegated API permission ===' -ForegroundColor Cyan
Write-Host "Environment: $Environment"
Write-Host "SPA:         $($spaConfig.displayName)"
Write-Host "API:         $($apiConfig.displayName)"
Write-Host "Scope:       $scopeValue"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

$SpaClientId = Resolve-B2cSpaClientId `
    -AccessToken $accessToken `
    -Environment $Environment `
    -SpaClientId $SpaClientId `
    -ManagementClientId $ManagementClientId `
    -RepoRoot $repoRoot

$spaApp = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications?`$filter=appId eq '$SpaClientId'" -ApiVersion v1.0
$spaObject = @($spaApp.value)[0]
if ($null -eq $spaObject) {
    throw "SPA app not found: $SpaClientId"
}

$apiAppMatch = Get-B2cApplicationByDisplayName -AccessToken $accessToken -DisplayName $apiConfig.displayName -ExcludeAppId $ManagementClientId
if ($null -eq $apiAppMatch) {
    throw "API app not found: $($apiConfig.displayName). Run register-b2c-api-app.ps1 first."
}

$apiApp = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications/$($apiAppMatch.id)" -ApiVersion v1.0
$scope = @($apiApp.api.oauth2PermissionScopes) | Where-Object { $_.value -eq $scopeValue } | Select-Object -First 1
if ($null -eq $scope) {
    throw "API scope '$scopeValue' not exposed on $($apiConfig.displayName)."
}

$requiredResourceAccess = @($spaObject.requiredResourceAccess)
$apiResource = $requiredResourceAccess | Where-Object { $_.resourceAppId -eq $apiApp.appId } | Select-Object -First 1

if ($null -eq $apiResource) {
    $requiredResourceAccess += [PSCustomObject]@{
        resourceAppId  = $apiApp.appId
        resourceAccess = @(
            [PSCustomObject]@{
                id   = $scope.id
                type = 'Scope'
            }
        )
    }
}
else {
    $existingAccess = @($apiResource.resourceAccess)
    if (@($existingAccess | Where-Object { $_.id -eq $scope.id }).Count -eq 0) {
        $existingAccess += [PSCustomObject]@{
            id   = $scope.id
            type = 'Scope'
        }
    }

    $apiResource.resourceAccess = $existingAccess
}

Write-Host "Granting scope $($scope.id) on SPA $($spaObject.displayName)..."
Invoke-B2cGraphApi -AccessToken $accessToken -Method Patch -Path "applications/$($spaObject.id)" -Body @{
    requiredResourceAccess = $requiredResourceAccess
} -ApiVersion v1.0 | Out-Null

Write-Host 'SPA delegated API permission granted.' -ForegroundColor Green
Write-Host 'Users may need to sign out and sign in again to pick up the new scope.'

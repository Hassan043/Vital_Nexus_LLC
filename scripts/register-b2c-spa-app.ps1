# Creates or updates the VitalNexus React frontend SPA app registration in Entra External ID via Microsoft Graph.
# Requires a management app with Application.ReadWrite.All in the B2C tenant.
#
# Usage:
#   $env:B2C_TENANT_ID = '<b2c-tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   .\scripts\register-b2c-spa-app.ps1 -Environment dev
#   .\scripts\register-b2c-spa-app.ps1 -Environment dev -KeyVaultName kv-vnx-dev-xxxxxxx

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,

    [string]$KeyVaultName = '',

    [switch]$SkipDeployedFrontendUri
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$managementClientId = $ManagementClientId.Trim()

$envConfigPath = Join-Path $repoRoot 'infra/identity/spa-app/environments.json'
$allEnvConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
$envConfig = $allEnvConfig.$Environment
$displayName = $envConfig.displayName

function Get-DeployedFrontendRedirectUri {
    param([string]$TargetEnvironment)

    $resourceGroup = "rg-vitalnexus-$TargetEnvironment"
    $namePrefix = "ca-vnx-frontend-$TargetEnvironment"

    try {
        $fqdn = az containerapp list `
            --resource-group $resourceGroup `
            --query "[?starts_with(name, '$namePrefix')].properties.configuration.ingress.fqdn | [0]" `
            -o tsv 2>$null
    }
    catch {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($fqdn)) {
        return $null
    }

    return "https://$fqdn/"
}

$redirectUris = [System.Collections.Generic.List[string]]::new()
foreach ($uri in $envConfig.redirectUris) {
    if (-not $redirectUris.Contains($uri)) {
        [void]$redirectUris.Add($uri)
    }
}

if ($envConfig.resolveDeployedFrontendUri -and -not $SkipDeployedFrontendUri) {
    $deployedUri = Get-DeployedFrontendRedirectUri -TargetEnvironment $Environment
    if ($null -ne $deployedUri -and -not $redirectUris.Contains($deployedUri)) {
        Write-Host "Including deployed frontend redirect URI: $deployedUri"
        [void]$redirectUris.Add($deployedUri)
    }
    elseif ($null -eq $deployedUri) {
        Write-Host "No deployed frontend Container App found; static redirect URIs only."
    }
}

Write-Host '=== Register Entra External ID SPA app ===' -ForegroundColor Cyan
Write-Host "Environment:   $Environment"
Write-Host "App name:      $displayName"
Write-Host "Redirect URIs: $($redirectUris -join ', ')"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

$filter = [uri]::EscapeDataString("displayName eq '$displayName'")
$existingApps = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications?`$filter=$filter" -ApiVersion v1.0

$existingSpaApp = $existingApps.value | Where-Object { $_.appId -ne $managementClientId } | Select-Object -First 1

if ($null -eq $existingSpaApp -and $existingApps.value.Count -gt 0) {
    $managementMatch = $existingApps.value | Where-Object { $_.appId -eq $managementClientId } | Select-Object -First 1
    if ($null -ne $managementMatch) {
        throw @"
An app named '$displayName' exists but its Client ID matches B2C_MANAGEMENT_CLIENT_ID.
Use separate app registrations:
  1. Management app (confidential) — e.g. 'VitalNexus B2C Management' with Graph Application.ReadWrite.All
  2. SPA app (public) — '$displayName' with SPA redirect URIs only
Rename the management app in the portal, then re-run this script.
"@
    }
}

if ($null -ne $existingSpaApp -and $existingSpaApp.spa.redirectUris) {
    foreach ($existingUri in $existingSpaApp.spa.redirectUris) {
        if (-not $redirectUris.Contains($existingUri)) {
            Write-Host "Preserving existing redirect URI: $existingUri"
            [void]$redirectUris.Add($existingUri)
        }
    }
}

$expandedRedirectUris = Expand-SpaRedirectUris -RedirectUris @($redirectUris)
$redirectUris = [System.Collections.Generic.List[string]]::new()
foreach ($uri in $expandedRedirectUris) {
    if (-not $redirectUris.Contains($uri)) {
        [void]$redirectUris.Add($uri)
    }
}

$appBody = @{
    displayName    = $displayName
    signInAudience = 'AzureADandPersonalMicrosoftAccount'
    spa            = @{
        redirectUris = @($redirectUris)
    }
}

if ($null -ne $existingSpaApp) {
    $objectId = $existingSpaApp.id
    $clientId = $existingSpaApp.appId
    Write-Host "Updating existing SPA registration: $displayName"
    Invoke-B2cGraphApi -AccessToken $accessToken -Method Patch -Path "applications/$objectId" -Body $appBody -ApiVersion v1.0 | Out-Null
}
else {
    Write-Host "Creating SPA registration: $displayName"
    $created = Invoke-B2cGraphApi -AccessToken $accessToken -Method Post -Path 'applications' -Body $appBody -ApiVersion v1.0
    $objectId = $created.id
    $clientId = $created.appId
}

if ($clientId -eq $managementClientId) {
    throw 'SPA Client ID must not match B2C_MANAGEMENT_CLIENT_ID. Create a separate SPA app registration.'
}

if (-not [string]::IsNullOrWhiteSpace($KeyVaultName)) {
    Write-Host "Writing b2c-spa-client-id to Key Vault: $KeyVaultName"
    az keyvault secret set `
        --vault-name $KeyVaultName `
        --name 'b2c-spa-client-id' `
        --value $clientId `
        --output none
}

Write-Host ''
Write-Host 'SPA registration complete.' -ForegroundColor Green
Write-Host "Object ID:  $objectId"
Write-Host "Client ID:  $clientId"
Write-Host ''
Write-Host 'Store Client ID as GitHub Environment secret B2C_SPA_CLIENT_ID and Key Vault secret b2c-spa-client-id.'

return $clientId

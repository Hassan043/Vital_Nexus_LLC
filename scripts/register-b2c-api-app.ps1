# Creates or updates the VitalNexus backend API app registration and exposes OAuth2 scopes in Entra External ID.
# Requires a management app with Application.ReadWrite.All in the B2C tenant.
#
# Usage:
#   $env:B2C_TENANT_ID = '<b2c-tenant-guid>'
#   $env:B2C_MANAGEMENT_CLIENT_ID = '<management-app-client-id>'
#   $env:B2C_MANAGEMENT_CLIENT_SECRET = '<management-app-secret>'
#   .\scripts\register-b2c-api-app.ps1 -Environment dev
#   .\scripts\register-b2c-api-app.ps1 -Environment dev -KeyVaultName kv-vnx-dev-xxxxxxx

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'test', 'prod')]
    [string]$Environment,

    [string]$TenantId = $env:B2C_TENANT_ID,
    [string]$ManagementClientId = $env:B2C_MANAGEMENT_CLIENT_ID,
    [string]$ManagementClientSecret = $env:B2C_MANAGEMENT_CLIENT_SECRET,

    [string]$KeyVaultName = ''
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Join-Path $scriptRoot '..'
. (Join-Path $scriptRoot 'b2c/B2cGraphCommon.ps1')

Assert-B2cManagementCredentials -TenantId $TenantId -ManagementClientId $ManagementClientId -ManagementClientSecret $ManagementClientSecret

$managementClientId = $ManagementClientId.Trim()

$envConfigPath = Join-Path $repoRoot 'infra/identity/api-app/environments.json'
$allEnvConfig = Get-Content $envConfigPath -Raw | ConvertFrom-Json
$envConfig = $allEnvConfig.$Environment
$displayName = $envConfig.displayName
$tenantDomain = "$($envConfig.tenantDomainPrefix).onmicrosoft.com"
$applicationIdUri = "https://$tenantDomain/$($envConfig.scopeNamespace)"

function Build-OAuth2PermissionScopes {
    param(
        [object[]]$ScopeDefinitions,
        [object[]]$ExistingScopes
    )

    $scopes = New-Object System.Collections.Generic.List[object]
    foreach ($definition in $ScopeDefinitions) {
        $existing = @($ExistingScopes) | Where-Object { $_.value -eq $definition.value } | Select-Object -First 1
        $scopeId = if ($null -ne $existing) { $existing.id } else { [guid]::NewGuid().ToString() }

        [void]$scopes.Add([PSCustomObject]@{
            adminConsentDescription    = [string]$definition.adminConsentDescription
            adminConsentDisplayName    = [string]$definition.adminConsentDisplayName
            id                         = [string]$scopeId
            isEnabled                  = $true
            type                       = 'User'
            userConsentDescription     = [string]$definition.userConsentDescription
            userConsentDisplayName     = [string]$definition.userConsentDisplayName
            value                      = [string]$definition.value
        })
    }

    return $scopes.ToArray()
}

function Set-ApplicationIdentifierUri {
    param(
        [string]$ObjectId,
        [string[]]$IdentifierUris,
        [int]$MaxAttempts = 6
    )

    $identifierBody = @{
        identifierUris = $IdentifierUris
    }

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            Write-Host "Setting Application ID URI (attempt $attempt/$MaxAttempts)..."
            Invoke-B2cGraphApi -AccessToken $accessToken -Method Patch -Path "applications/$ObjectId" -Body $identifierBody -ApiVersion v1.0 | Out-Null
            return
        }
        catch {
            $isNotFound = $_.Exception.Message -match 'ResourceNotFound|NotFound|does not exist'
            if ($isNotFound -and $attempt -lt $MaxAttempts) {
                $waitSeconds = $attempt * 5
                Write-Host "App not yet available in Graph; waiting ${waitSeconds}s before retry..."
                Start-Sleep -Seconds $waitSeconds
                continue
            }

            throw
        }
    }
}

function Set-ApplicationOAuth2Scopes {
    param(
        [string]$ObjectId,
        [object[]]$Scopes,
        [int]$MaxAttempts = 3
    )

    $scopeBody = [PSCustomObject]@{
        api = [PSCustomObject]@{
            requestedAccessTokenVersion = 2
            oauth2PermissionScopes      = @($Scopes)
        }
    }

    $scopeJson = $scopeBody | ConvertTo-Json -Depth 10 -Compress
    Write-Host "Scope payload: $scopeJson"

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            Write-Host "Exposing OAuth2 scopes (attempt $attempt/$MaxAttempts)..."
            Invoke-RestMethod `
                -Method Patch `
                -Uri "https://graph.microsoft.com/v1.0/applications/$ObjectId" `
                -Headers @{ Authorization = "Bearer $accessToken"; 'Content-Type' = 'application/json' } `
                -Body $scopeJson | Out-Null
            return
        }
        catch {
            $details = $_.ErrorDetails.Message
            if ([string]::IsNullOrWhiteSpace($details)) {
                $details = $_.Exception.Message
            }

            if ($attempt -lt $MaxAttempts) {
                Write-Host "Scope update failed; retrying in 5s. $details"
                Start-Sleep -Seconds 5
                continue
            }

            throw "Graph API Patch applications/$ObjectId scopes failed: $details"
        }
    }
}

function Set-ApiApplicationConfiguration {
    param(
        [string]$ObjectId,
        [string[]]$IdentifierUris,
        [object[]]$Scopes,
        [string[]]$ExistingIdentifierUris = @()
    )

    $needsIdentifierUri = $ExistingIdentifierUris.Count -eq 0 -or ($IdentifierUris | Where-Object { $ExistingIdentifierUris -contains $_ }).Count -eq 0
    if ($needsIdentifierUri) {
        Set-ApplicationIdentifierUri -ObjectId $ObjectId -IdentifierUris $IdentifierUris
    }
    else {
        Write-Host 'Application ID URI already configured.'
    }

    Set-ApplicationOAuth2Scopes -ObjectId $ObjectId -Scopes $Scopes
}

Write-Host '=== Register Entra External ID API app ===' -ForegroundColor Cyan
Write-Host "Environment:         $Environment"
Write-Host "App name:            $displayName"
Write-Host "Application ID URI:  $applicationIdUri"
Write-Host ''

$accessToken = Get-B2cGraphAccessToken -TenantId $TenantId -ClientId $ManagementClientId -ClientSecret $ManagementClientSecret

$filter = [uri]::EscapeDataString("displayName eq '$displayName'")
$existingApps = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications?`$filter=$filter" -ApiVersion v1.0

$existingApiApp = $existingApps.value | Where-Object { $_.appId -ne $managementClientId } | Select-Object -First 1
$existingScopes = @()
$existingIdentifierUris = @()

if ($null -ne $existingApiApp) {
    $fullApp = Invoke-B2cGraphApi -AccessToken $accessToken -Method Get -Path "applications/$($existingApiApp.id)" -ApiVersion v1.0
    if ($null -ne $fullApp.api -and $null -ne $fullApp.api.oauth2PermissionScopes) {
        $existingScopes = @($fullApp.api.oauth2PermissionScopes)
    }
    if ($null -ne $fullApp.identifierUris) {
        $existingIdentifierUris = @($fullApp.identifierUris)
    }
}

$oauth2PermissionScopes = Build-OAuth2PermissionScopes -ScopeDefinitions @($envConfig.scopes) -ExistingScopes $existingScopes

if ($null -ne $existingApiApp) {
    $objectId = $existingApiApp.id
    $clientId = $existingApiApp.appId
    Write-Host "Updating existing API registration: $displayName"
    Invoke-B2cGraphApi -AccessToken $accessToken -Method Patch -Path "applications/$objectId" -Body @{
        signInAudience = 'AzureADMyOrg'
    } -ApiVersion v1.0 | Out-Null
    Set-ApiApplicationConfiguration -ObjectId $objectId -IdentifierUris @($applicationIdUri) -Scopes $oauth2PermissionScopes -ExistingIdentifierUris $existingIdentifierUris
}
else {
    Write-Host "Creating API registration: $displayName"
    $created = Invoke-B2cGraphApi -AccessToken $accessToken -Method Post -Path 'applications' -Body @{
        displayName    = $displayName
        signInAudience = 'AzureADMyOrg'
    } -ApiVersion v1.0

    $objectId = $created.id
    $clientId = $created.appId
    Write-Host "Created app object $objectId (client ID $clientId)."
    Write-Host 'Configuring Application ID URI and scopes...'
    Set-ApiApplicationConfiguration -ObjectId $objectId -IdentifierUris @($applicationIdUri) -Scopes $oauth2PermissionScopes
}

if ($clientId -eq $managementClientId) {
    throw 'API Client ID must not match B2C_MANAGEMENT_CLIENT_ID. Create a separate API app registration.'
}

$scopeValues = @($envConfig.scopes | ForEach-Object { $_.value })
$scopeUri = "$applicationIdUri/$($scopeValues[0])"

if (-not [string]::IsNullOrWhiteSpace($KeyVaultName)) {
    Write-Host "Writing API identity secrets to Key Vault: $KeyVaultName"
    az keyvault secret set --vault-name $KeyVaultName --name 'b2c-api-client-id' --value $clientId --output none
    az keyvault secret set --vault-name $KeyVaultName --name 'b2c-api-application-id-uri' --value $applicationIdUri --output none
    az keyvault secret set --vault-name $KeyVaultName --name 'b2c-api-scope' --value $scopeValues[0] --output none
    az keyvault secret set --vault-name $KeyVaultName --name 'b2c-api-scope-uri' --value $scopeUri --output none
}

Write-Host ''
Write-Host 'API registration complete.' -ForegroundColor Green
Write-Host "Object ID:            $objectId"
Write-Host "Client ID:            $clientId"
Write-Host "Application ID URI:   $applicationIdUri"
Write-Host "Primary scope URI:    $scopeUri"
Write-Host ''
Write-Host 'Store Client ID as GitHub Environment secret B2C_API_CLIENT_ID.'
Write-Host 'Grant SPA delegated access to this scope in F3.T1.4.'

return @{
    ClientId           = $clientId
    ApplicationIdUri   = $applicationIdUri
    ScopeUri           = $scopeUri
}

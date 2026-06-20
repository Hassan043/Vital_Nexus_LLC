$ErrorActionPreference = 'Stop'

function Get-B2cGraphAccessToken {
    param(
        [string]$TenantId,
        [string]$ClientId,
        [string]$ClientSecret
    )

    $body = @{
        grant_type    = 'client_credentials'
        client_id     = $ClientId.Trim()
        client_secret = $ClientSecret.Trim()
        scope         = 'https://graph.microsoft.com/.default'
    }

    $tokenResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "https://login.microsoftonline.com/$TenantId/oauth2/v2.0/token" `
        -Body $body `
        -ContentType 'application/x-www-form-urlencoded'

    return $tokenResponse.access_token
}

function Invoke-B2cGraphApi {
    param(
        [string]$AccessToken,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [ValidateSet('beta', 'v1.0')]
        [string]$ApiVersion = 'v1.0'
    )

    $uri = "https://graph.microsoft.com/$ApiVersion/$Path"
    $params = @{
        Method  = $Method
        Uri     = $uri
        Headers = @{ Authorization = "Bearer $AccessToken" }
    }

    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }

    try {
        return Invoke-RestMethod @params
    }
    catch {
        $response = $_.Exception.Response
        if ($null -ne $response) {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            $details = $reader.ReadToEnd()
            if ($details -match 'Authorization_RequestDenied' -and $Path -match 'branding') {
                throw @"
Graph API $Method $Path failed: insufficient privileges.
In the EXTERNAL tenant: add OrganizationalBranding.ReadWrite.All (application) to the management app and grant admin consent.
Details: $details
"@
            }

            throw "Graph API $Method $Path failed: $details"
        }

        throw
    }
}

function Test-B2cUserFlowExists {
    param(
        [string]$AccessToken,
        [string]$UserFlowId
    )

    try {
        Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path "identity/b2cUserFlows/$UserFlowId" -ApiVersion beta | Out-Null
        return $true
    }
    catch {
        if ($_.Exception.Message -match 'NotFound|404|ResourceNotFound|CIAM directory|cannot be made by an Azure AD CIAM') {
            return $false
        }

        throw
    }
}

function Get-CiamAuthenticationEventsFlows {
    param(
        [string]$AccessToken
    )

    $response = Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path 'identity/authenticationEventsFlows' -ApiVersion v1.0
    return @($response.value)
}

function Get-CiamUserFlowByDisplayName {
    param(
        [string]$AccessToken,
        [string]$DisplayName
    )

    $flows = Get-CiamAuthenticationEventsFlows -AccessToken $AccessToken
    return $flows | Where-Object { $_.displayName -eq $DisplayName } | Select-Object -First 1
}

function New-CiamUserFlowIfMissing {
    param(
        [string]$AccessToken,
        [string]$DisplayName,
        [string]$SpaClientId = ''
    )

    $existing = Get-CiamUserFlowByDisplayName -AccessToken $AccessToken -DisplayName $DisplayName
    if ($null -ne $existing) {
        Write-Host "User flow already exists: $DisplayName ($($existing.id))"
        return $existing
    }

    $flowBody = @{
        '@odata.type'                   = '#microsoft.graph.externalUsersSelfServiceSignUpEventsFlow'
        displayName                     = $DisplayName
        onAuthenticationMethodLoadStart = @{
            '@odata.type'       = '#microsoft.graph.onAuthenticationMethodLoadStartExternalUsersSelfServiceSignUp'
            identityProviders   = @(
                @{ id = 'EmailPassword-OAUTH' }
            )
        }
        onInteractiveAuthFlowStart    = @{
            '@odata.type'     = '#microsoft.graph.onInteractiveAuthFlowStartExternalUsersSelfServiceSignUp'
            isSignUpAllowed   = $true
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($SpaClientId)) {
        $flowBody.conditions = @{
            applications = @{
                includeApplications = @(
                    @{ appId = $SpaClientId }
                )
            }
        }
    }

    Write-Host "Creating CIAM user flow: $DisplayName"
    $created = Invoke-B2cGraphApi -AccessToken $AccessToken -Method Post -Path 'identity/authenticationEventsFlows' -Body $flowBody -ApiVersion v1.0
    Write-Host "Created CIAM user flow: $DisplayName ($($created.id))"
    return $created
}

function New-B2cUserFlowIfMissing {
    param(
        [string]$AccessToken,
        [string]$UserFlowId,
        [object]$FlowTemplate
    )

    if (Test-B2cUserFlowExists -AccessToken $AccessToken -UserFlowId $UserFlowId) {
        Write-Host "User flow already exists: $UserFlowId"
        return
    }

    Write-Host "Creating user flow: $UserFlowId"
    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Post -Path 'identity/b2cUserFlows' -Body $FlowTemplate -ApiVersion beta | Out-Null
    Write-Host "Created user flow: $UserFlowId"
}

function Enable-B2cUserFlowEnglish {
    param(
        [string]$AccessToken,
        [string]$UserFlowId
    )

    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Patch -Path "identity/b2cUserFlows/$UserFlowId" -Body @{
        isLanguageCustomizationEnabled = $true
        defaultLanguageTag             = 'en'
    } -ApiVersion beta | Out-Null

    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Put -Path "identity/b2cUserFlows/$UserFlowId/languages/en" -Body @{
        id        = 'en'
        isEnabled = $true
    } -ApiVersion beta | Out-Null
}

function Assert-B2cManagementCredentials {
    param(
        [string]$TenantId,
        [string]$ManagementClientId,
        [string]$ManagementClientSecret
    )

    $TenantId = $TenantId.Trim()
    $ManagementClientId = $ManagementClientId.Trim()
    $ManagementClientSecret = $ManagementClientSecret.Trim()

    if ([string]::IsNullOrWhiteSpace($TenantId) -or
        [string]::IsNullOrWhiteSpace($ManagementClientId) -or
        [string]::IsNullOrWhiteSpace($ManagementClientSecret)) {
        throw 'Set B2C_TENANT_ID, B2C_MANAGEMENT_CLIENT_ID, and B2C_MANAGEMENT_CLIENT_SECRET.'
    }
}

function Invoke-B2cGraphBrandingApi {
    param(
        [string]$AccessToken,
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$AcceptLanguage = '0'
    )

    $uri = "https://graph.microsoft.com/v1.0/$Path"
    $headers = @{
        Authorization     = "Bearer $AccessToken"
        'Accept-Language' = $AcceptLanguage
    }

    $params = @{
        Method  = $Method
        Uri     = $uri
        Headers = $headers
    }

    if ($null -ne $Body) {
        $params.ContentType = 'application/json'
        $params.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }

    try {
        return Invoke-RestMethod @params
    }
    catch {
        $response = $_.Exception.Response
        if ($null -ne $response) {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
            $details = $reader.ReadToEnd()
            if ($details -match 'Authorization_RequestDenied') {
                throw @"
Graph API $Method $Path failed: insufficient privileges.
In the EXTERNAL tenant (not your work tenant): App registrations -> VitalNexus B2C Management Dev -> API permissions -> add Microsoft Graph application permission OrganizationalBranding.ReadWrite.All -> Grant admin consent.
Confirm B2C_MANAGEMENT_CLIENT_ID is the management app (7392a9b0-...), not the SPA app. Wait 1-2 minutes after consent, then re-run.
Details: $details
"@
            }

            throw "Graph API $Method $Path failed: $details"
        }

        throw
    }
}

function New-ContentCustomizationBody {
    param(
        [hashtable]$ContentCustomization
    )

    if ($null -eq $ContentCustomization -or $ContentCustomization.Count -eq 0) {
        return $null
    }

    $attributeCollection = foreach ($key in ($ContentCustomization.Keys | Sort-Object)) {
        @{
            key   = $key
            value = [string]$ContentCustomization[$key]
        }
    }

    return @{
        '@odata.type'         = 'microsoft.graph.contentCustomization'
        attributeCollection   = @($attributeCollection)
    }
}

function Test-B2cBrandingApiAccess {
    param(
        [string]$AccessToken,
        [string]$OrganizationId
    )

    try {
        Invoke-B2cGraphBrandingApi -AccessToken $AccessToken -Method Get -Path "organization/$OrganizationId/branding" -AcceptLanguage '0' | Out-Null
        return $true
    }
    catch {
        if ($_.Exception.Message -match 'ResourceNotFound|404|does not exist') {
            return $false
        }

        throw
    }
}

function Get-OrganizationBranding {
    param(
        [string]$AccessToken,
        [string]$OrganizationId
    )

    return Invoke-B2cGraphBrandingApi -AccessToken $AccessToken -Method Get -Path "organization/$OrganizationId/branding" -AcceptLanguage '0'
}

function Get-OrganizationBrandingLocalization {
    param(
        [string]$AccessToken,
        [string]$OrganizationId,
        [string]$Locale
    )

    try {
        return Invoke-B2cGraphBrandingApi -AccessToken $AccessToken -Method Get -Path "organization/$OrganizationId/branding/localizations/$Locale" -AcceptLanguage $Locale
    }
    catch {
        if ($_.Exception.Message -match 'NotFound|404|ResourceNotFound') {
            return $null
        }

        throw
    }
}

function Set-OrganizationBrandingStrings {
    param(
        [string]$AccessToken,
        [string]$OrganizationId,
        [hashtable]$BrandingProperties
    )

    if ($BrandingProperties.Count -eq 0) {
        return
    }

    try {
        Invoke-B2cGraphBrandingApi -AccessToken $AccessToken -Method Patch -Path "organization/$OrganizationId/branding" -Body $BrandingProperties -AcceptLanguage '0' | Out-Null
    }
    catch {
        if ($_.Exception.Message -match 'ResourceNotFound|404|does not exist') {
            Write-Warning 'Default company branding is not initialized yet; skipped default branding PATCH (localization POST/PATCH creates it).'
            return
        }

        throw
    }
}

function Set-OrganizationBrandingLocalizationStrings {
    param(
        [string]$AccessToken,
        [string]$OrganizationId,
        [string]$Locale,
        [hashtable]$BrandingProperties
    )

    if ($BrandingProperties.Count -eq 0) {
        return
    }

    $existing = Get-OrganizationBrandingLocalization -AccessToken $AccessToken -OrganizationId $OrganizationId -Locale $Locale
    if ($null -eq $existing) {
        $createBody = @{ id = $Locale }
        foreach ($key in $BrandingProperties.Keys) {
            $createBody[$key] = $BrandingProperties[$key]
        }

        Invoke-B2cGraphBrandingApi -AccessToken $AccessToken -Method Post -Path "organization/$OrganizationId/branding/localizations" -Body $createBody -AcceptLanguage $Locale | Out-Null
        return
    }

    Invoke-B2cGraphBrandingApi -AccessToken $AccessToken -Method Patch -Path "organization/$OrganizationId/branding/localizations/$Locale" -Body $BrandingProperties -AcceptLanguage $Locale | Out-Null
}

function Set-OrganizationBrandingStream {
    param(
        [string]$AccessToken,
        [string]$OrganizationId,
        [string]$PropertyName,
        [string]$FilePath,
        [string]$Locale = 'en'
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        throw "Branding asset not found: $FilePath"
    }

    $extension = [System.IO.Path]::GetExtension($FilePath).ToLowerInvariant()
    $contentType = switch ($extension) {
        '.png' { 'image/png' }
        '.jpg' { 'image/jpeg' }
        '.jpeg' { 'image/jpeg' }
        '.ico' { 'image/x-icon' }
        default { throw "Unsupported branding asset type '$extension' for $PropertyName. Use PNG, JPEG, or ICO." }
    }

    $uri = "https://graph.microsoft.com/v1.0/organization/$OrganizationId/branding/localizations/$Locale/$PropertyName"
    Invoke-RestMethod `
        -Method Put `
        -Uri $uri `
        -Headers @{
            Authorization     = "Bearer $AccessToken"
            'Accept-Language' = $Locale
        } `
        -ContentType $contentType `
        -InFile $FilePath | Out-Null
}

function Get-B2cUserFlowOverridePages {
    param(
        [string]$AccessToken,
        [string]$UserFlowId,
        [string]$LanguageId = 'en'
    )

    $response = Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path "identity/b2cUserFlows/$UserFlowId/languages/$LanguageId/overridesPages" -ApiVersion beta
    return @($response.value)
}

function Set-B2cUserFlowLanguageOverridePageContent {
    param(
        [string]$AccessToken,
        [string]$UserFlowId,
        [string]$LanguageId,
        [string]$PageId,
        [object[]]$LocalizedStrings
    )

    $body = @{
        localizedStrings = $LocalizedStrings
    }

    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Patch -Path "identity/b2cUserFlows/$UserFlowId/languages/$LanguageId/overridesPages/$PageId" -Body $body -ApiVersion beta | Out-Null
}

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
            if ($details -match 'Authorization_RequestDenied|AccessDenied|accessDenied' -and $Path -match 'conditionalAccess') {
                throw @"
Graph API $Method $Path failed: insufficient privileges for Conditional Access.
In vitalnexusexternal: App registrations -> VitalNexus B2C Management Dev -> API permissions -> add ALL THREE application permissions, then Grant admin consent:
  - Policy.Read.All
  - Policy.ReadWrite.ConditionalAccess
  - Application.Read.All
Microsoft requires all three for app-only Conditional Access read/write. Wait 2-5 minutes after consent, then re-run.
Details: $details
"@
            }

            if ($details -match 'Authorization_RequestDenied|accessDenied' -and $Path -match 'authenticationMethodsPolicy') {
                throw @"
Graph API $Method $Path failed: insufficient privileges.
In the EXTERNAL tenant: add Microsoft Graph application permission Policy.ReadWrite.AuthenticationMethod to VitalNexus B2C Management Dev, grant admin consent, wait 1-2 minutes, then re-run.
Details: $details
"@
            }

            if ($details -match 'Authorization_RequestDenied' -and $Path -match 'branding') {
                throw @"
Graph API $Method $Path failed: insufficient privileges.
In the EXTERNAL tenant: add OrganizationalBranding.ReadWrite.All (application) to the management app and grant admin consent.
Details: $details
"@
            }

            if ($details -match 'Security Defaults is enabled') {
                throw @"
Graph API $Method $Path failed: Security Defaults is enabled in this tenant.
Conditional Access policies cannot be created until Security Defaults is disabled.
Re-run configure script (it disables Security Defaults automatically), or in Entra portal:
  Entra ID -> Properties -> Manage security defaults -> Disabled
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

function Get-CiamUserFlowById {
    param(
        [string]$AccessToken,
        [string]$FlowId
    )

    return Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path "identity/authenticationEventsFlows/$FlowId" -ApiVersion v1.0
}

function Get-CiamUserFlowLinkedAppIds {
    param(
        [string]$AccessToken,
        [string]$FlowId
    )

    $flow = Get-CiamUserFlowById -AccessToken $AccessToken -FlowId $FlowId
    $linkedApps = @()
    if ($null -ne $flow.conditions -and $null -ne $flow.conditions.applications -and $null -ne $flow.conditions.applications.includeApplications) {
        $linkedApps = @($flow.conditions.applications.includeApplications | ForEach-Object { $_.appId })
    }

    return @($linkedApps | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Add-CiamUserFlowIncludeApplication {
    param(
        [string]$AccessToken,
        [string]$FlowId,
        [string]$SpaClientId
    )

    $body = @{
        '@odata.type' = '#microsoft.graph.authenticationConditionApplication'
        appId         = $SpaClientId.Trim()
    }

    try {
        Invoke-B2cGraphApi -AccessToken $AccessToken -Method Post -Path "identity/authenticationEventsFlows/$FlowId/conditions/applications/includeApplications" -Body $body -ApiVersion v1.0 | Out-Null
        return $true
    }
    catch {
        if ($_.Exception.Message -match 'already exists|ObjectConflict|conflict') {
            return $false
        }

        throw
    }
}

function Ensure-CiamUserFlowSpaLink {
    param(
        [string]$AccessToken,
        [string]$FlowId,
        [string]$DisplayName,
        [string]$SpaClientId
    )

    if ([string]::IsNullOrWhiteSpace($SpaClientId)) {
        throw 'B2C_SPA_CLIENT_ID is required to link the VitalNexus Frontend app to the CIAM user flow.'
    }

    $linkedAppIds = Get-CiamUserFlowLinkedAppIds -AccessToken $AccessToken -FlowId $FlowId
    if ($linkedAppIds -contains $SpaClientId.Trim()) {
        Write-Host "SPA already linked to user flow: $DisplayName ($FlowId)"
        return
    }

    Write-Host "Linking SPA $SpaClientId to user flow: $DisplayName ($FlowId)"
    $added = Add-CiamUserFlowIncludeApplication -AccessToken $AccessToken -FlowId $FlowId -SpaClientId $SpaClientId
    if ($added) {
        Write-Host 'SPA linked to user flow.' -ForegroundColor Green
    }
    else {
        Write-Host 'SPA link already present (Graph reported conflict).' -ForegroundColor Green
    }
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
        if (-not [string]::IsNullOrWhiteSpace($SpaClientId)) {
            Ensure-CiamUserFlowSpaLink -AccessToken $AccessToken -FlowId $existing.id -DisplayName $DisplayName -SpaClientId $SpaClientId
        }
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
    if (-not [string]::IsNullOrWhiteSpace($SpaClientId)) {
        Ensure-CiamUserFlowSpaLink -AccessToken $AccessToken -FlowId $created.id -DisplayName $DisplayName -SpaClientId $SpaClientId
    }
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

function Get-B2cApplicationByDisplayName {
    param(
        [string]$AccessToken,
        [string]$DisplayName,
        [string]$ExcludeAppId = ''
    )

    $filter = [uri]::EscapeDataString("displayName eq '$DisplayName'")
    $existingApps = Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path "applications?`$filter=$filter" -ApiVersion v1.0
    $apps = @($existingApps.value)

    if (-not [string]::IsNullOrWhiteSpace($ExcludeAppId)) {
        $apps = @($apps | Where-Object { $_.appId -ne $ExcludeAppId.Trim() })
    }

    return $apps | Select-Object -First 1
}

function Resolve-B2cSpaClientId {
    param(
        [string]$AccessToken,
        [string]$Environment,
        [string]$SpaClientId = '',
        [string]$ManagementClientId = '',
        [string]$RepoRoot = ''
    )

    $SpaClientId = $SpaClientId.Trim()
    if (-not [string]::IsNullOrWhiteSpace($SpaClientId)) {
        return $SpaClientId
    }

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        throw @"
B2C_SPA_CLIENT_ID is not set.
Add to .env from your F3.T1.2 VitalNexus Frontend app registration (Application client ID), reload env, and re-run.
"@
    }

    $spaConfigPath = Join-Path $RepoRoot 'infra/identity/spa-app/environments.json'
    $spaConfig = (Get-Content $spaConfigPath -Raw | ConvertFrom-Json).$Environment
    $displayName = $spaConfig.displayName

    Write-Host "B2C_SPA_CLIENT_ID not set; resolving SPA app by display name: $displayName"
    $spaApp = Get-B2cApplicationByDisplayName -AccessToken $AccessToken -DisplayName $displayName -ExcludeAppId $ManagementClientId

    if ($null -eq $spaApp -or [string]::IsNullOrWhiteSpace($spaApp.appId)) {
        throw @"
Could not resolve SPA client ID for '$displayName'.
Set B2C_SPA_CLIENT_ID in .env or run .\scripts\register-b2c-spa-app.ps1 -Environment $Environment first.
"@
    }

    Write-Host "Resolved SPA client ID: $($spaApp.appId)"
    return [string]$spaApp.appId
}

function Get-EmailOtpAuthenticationMethodConfiguration {
    param(
        [string]$AccessToken
    )

    return Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path 'policies/authenticationMethodsPolicy/authenticationMethodConfigurations/email' -ApiVersion v1.0
}

function Enable-ExternalIdEmailOtpAuthenticationMethod {
    param(
        [string]$AccessToken,
        [string]$Purpose = 'authentication and MFA'
    )

    $body = @{
        '@odata.type'                = '#microsoft.graph.emailAuthenticationMethodConfiguration'
        state                        = 'enabled'
        allowExternalIdToUseEmailOtp = 'enabled'
        includeTargets               = @(
            @{
                targetType = 'group'
                id         = 'all_users'
            }
        )
    }

    Write-Host "Enabling Email OTP authentication method for $Purpose..."
    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Patch -Path 'policies/authenticationMethodsPolicy/authenticationMethodConfigurations/email' -Body $body -ApiVersion v1.0 | Out-Null
}

function Enable-ExternalIdEmailOtpForPasswordRecovery {
    param(
        [string]$AccessToken
    )

    Enable-ExternalIdEmailOtpAuthenticationMethod -AccessToken $AccessToken -Purpose 'password recovery and MFA'
}

function Enable-ExternalIdEmailOtpForPasswordReset {
    param(
        [string]$AccessToken
    )

    Enable-ExternalIdEmailOtpForPasswordRecovery -AccessToken $AccessToken
}

function Set-PasswordResetLinkBranding {
    param(
        [string]$AccessToken,
        [string]$OrganizationId,
        [string]$Locale,
        [bool]$ShowForgotPasswordLink,
        [string]$CustomForgotMyPasswordText = ''
    )

    $visibility = @{
        '@odata.type'               = 'microsoft.graph.loginPageTextVisibilitySettings'
        hideAccountResetCredentials = -not $ShowForgotPasswordLink
        hideForgotMyPassword        = -not $ShowForgotPasswordLink
        hideCannotAccessYourAccount = -not $ShowForgotPasswordLink
        hideResetItNow              = -not $ShowForgotPasswordLink
    }

    $brandingBody = @{
        loginPageTextVisibilitySettings = $visibility
    }

    if (-not [string]::IsNullOrWhiteSpace($CustomForgotMyPasswordText)) {
        $brandingBody.customForgotMyPasswordText = $CustomForgotMyPasswordText
    }

    Write-Host 'Updating sign-in page password recovery link visibility...'
    Set-OrganizationBrandingLocalizationStrings -AccessToken $AccessToken -OrganizationId $OrganizationId -Locale $Locale -BrandingProperties $brandingBody

    if (Test-B2cBrandingApiAccess -AccessToken $AccessToken -OrganizationId $OrganizationId) {
        Set-OrganizationBrandingStrings -AccessToken $AccessToken -OrganizationId $OrganizationId -BrandingProperties $brandingBody
    }
}

function New-B2cPasswordResetUserFlowIfMissing {
    param(
        [string]$AccessToken,
        [string]$UserFlowId,
        [object]$FlowTemplate
    )

    if (Test-B2cUserFlowExists -AccessToken $AccessToken -UserFlowId $UserFlowId) {
        Write-Host "Password reset user flow already exists: $UserFlowId"
        return
    }

    Write-Host "Creating password reset user flow: $UserFlowId"
    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Post -Path 'identity/b2cUserFlows' -Body $FlowTemplate -ApiVersion beta | Out-Null
    Write-Host "Created password reset user flow: $UserFlowId"
}

function Get-IdentitySecurityDefaultsPolicy {
    param(
        [string]$AccessToken
    )

    return Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path 'policies/identitySecurityDefaultsEnforcementPolicy' -ApiVersion v1.0
}

function Disable-IdentitySecurityDefaultsIfEnabled {
    param(
        [string]$AccessToken,
        [int]$MaxWaitSeconds = 60
    )

    $policy = Get-IdentitySecurityDefaultsPolicy -AccessToken $AccessToken
    if ($policy.isEnabled -ne $true) {
        Write-Host 'Security Defaults already disabled.'
        return
    }

    Write-Host 'Security Defaults is enabled; disabling so Conditional Access policies can be used...' -ForegroundColor Yellow
    Invoke-B2cGraphApi -AccessToken $AccessToken -Method Patch -Path 'policies/identitySecurityDefaultsEnforcementPolicy' -Body @{
        isEnabled = $false
    } -ApiVersion v1.0 | Out-Null

    $deadline = (Get-Date).AddSeconds($MaxWaitSeconds)
    do {
        Start-Sleep -Seconds 3
        $policy = Get-IdentitySecurityDefaultsPolicy -AccessToken $AccessToken
        if ($policy.isEnabled -ne $true) {
            Write-Host 'Security Defaults disabled.' -ForegroundColor Green
            return
        }
    } while ((Get-Date) -lt $deadline)

    throw 'Security Defaults PATCH succeeded but tenant still reports isEnabled=true. Wait 1-2 minutes and re-run configure.'
}

function Get-ConditionalAccessPolicies {
    param(
        [string]$AccessToken
    )

    $response = Invoke-B2cGraphApi -AccessToken $AccessToken -Method Get -Path 'identity/conditionalAccess/policies' -ApiVersion v1.0
    return @($response.value)
}

function Get-ConditionalAccessPolicyByDisplayName {
    param(
        [string]$AccessToken,
        [string]$DisplayName
    )

    $policies = Get-ConditionalAccessPolicies -AccessToken $AccessToken
    return $policies | Where-Object { $_.displayName -eq $DisplayName } | Select-Object -First 1
}

function New-ConditionalAccessMfaPolicyBody {
    param(
        [string]$DisplayName,
        [string]$SpaClientId,
        [string[]]$BuiltInControls = @('mfa'),
        [string]$PolicyState = 'enabled',
        [bool]$IncludeAllUsers = $true,
        [bool]$TargetSpaApplication = $true
    )

    $applications = @{}
    if ($TargetSpaApplication) {
        if ([string]::IsNullOrWhiteSpace($SpaClientId)) {
            throw 'B2C_SPA_CLIENT_ID is required when targetSpaApplication is true.'
        }

        $applications.includeApplications = @($SpaClientId.Trim())
    }
    else {
        $applications.includeApplications = @('All')
    }

    $users = @{}
    if ($IncludeAllUsers) {
        $users.includeUsers = @('All')
    }

    return @{
        displayName   = $DisplayName
        state         = $PolicyState
        conditions    = @{
            clientAppTypes = @('browser', 'mobileAppsAndDesktopClients')
            users          = $users
            applications   = $applications
        }
        grantControls = @{
            operator          = 'OR'
            builtInControls   = @($BuiltInControls)
        }
    }
}

function Set-ConditionalAccessMfaPolicyForApplication {
    param(
        [string]$AccessToken,
        [string]$DisplayName,
        [string]$SpaClientId,
        [string[]]$BuiltInControls = @('mfa'),
        [string]$PolicyState = 'enabled',
        [bool]$IncludeAllUsers = $true,
        [bool]$TargetSpaApplication = $true
    )

    $policyBody = New-ConditionalAccessMfaPolicyBody `
        -DisplayName $DisplayName `
        -SpaClientId $SpaClientId `
        -BuiltInControls $BuiltInControls `
        -PolicyState $PolicyState `
        -IncludeAllUsers $IncludeAllUsers `
        -TargetSpaApplication $TargetSpaApplication

    $existing = Get-ConditionalAccessPolicyByDisplayName -AccessToken $AccessToken -DisplayName $DisplayName
    if ($null -ne $existing) {
        Write-Host "Updating Conditional Access policy: $DisplayName ($($existing.id))"
        Invoke-B2cGraphApi -AccessToken $AccessToken -Method Patch -Path "identity/conditionalAccess/policies/$($existing.id)" -Body $policyBody -ApiVersion v1.0 | Out-Null
        return $existing.id
    }

    Write-Host "Creating Conditional Access policy: $DisplayName"
    $created = Invoke-B2cGraphApi -AccessToken $AccessToken -Method Post -Path 'identity/conditionalAccess/policies' -Body $policyBody -ApiVersion v1.0
    Write-Host "Created Conditional Access policy: $DisplayName ($($created.id))"
    return $created.id
}

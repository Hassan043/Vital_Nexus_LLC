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

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

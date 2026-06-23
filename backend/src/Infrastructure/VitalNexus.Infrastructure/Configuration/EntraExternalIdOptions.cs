namespace VitalNexus.Infrastructure.Configuration;

public enum EntraExternalIdTenantKind
{
    Ciam,
    B2c,
}

public sealed class EntraExternalIdOptions
{
    public const string SectionName = "EntraExternalId";

    public string TenantId { get; set; } = string.Empty;

    public string TenantDomainPrefix { get; set; } = string.Empty;

    public string ApiClientId { get; set; } = string.Empty;

    public string ApplicationIdUri { get; set; } = string.Empty;

    public string RequiredScope { get; set; } = "access_as_user";

    public string[] AllowedOrigins { get; set; } = [];

    public EntraExternalIdTenantKind TenantKind { get; set; } = EntraExternalIdTenantKind.Ciam;

    public string UserFlowId { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(TenantDomainPrefix)
        && !string.IsNullOrWhiteSpace(ApiClientId);

    public string Authority => BuildAuthority();

    public IReadOnlyList<string> GetValidAudiences()
    {
        var audiences = new List<string> { ApiClientId.Trim() };

        if (!string.IsNullOrWhiteSpace(ApplicationIdUri))
        {
            audiences.Add(ApplicationIdUri.Trim());
        }

        return audiences;
    }

    public string BuildAuthority()
    {
        var tenantId = TenantId.Trim();
        var domainPrefix = TenantDomainPrefix.Trim();

        if (TenantKind == EntraExternalIdTenantKind.B2c)
        {
            var userFlow = UserFlowId.Trim();
            if (string.IsNullOrWhiteSpace(userFlow))
            {
                throw new InvalidOperationException(
                    "EntraExternalId:UserFlowId is required for legacy B2C tenants.");
            }

            return $"https://{domainPrefix}.b2clogin.com/{domainPrefix}.onmicrosoft.com/{userFlow}/v2.0";
        }

        return $"https://{domainPrefix}.ciamlogin.com/{tenantId}/v2.0";
    }
}

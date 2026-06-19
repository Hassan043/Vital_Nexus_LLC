namespace VitalNexus.Infrastructure.Configuration;

public sealed class EntraExternalIdOptions
{
    public const string SectionName = "EntraExternalId";

    public string TenantId { get; set; } = string.Empty;

    public string TenantDomainPrefix { get; set; } = string.Empty;

    public string ApiClientId { get; set; } = string.Empty;

    public string ApplicationIdUri { get; set; } = string.Empty;

    public string RequiredScope { get; set; } = "access_as_user";

    public string[] AllowedOrigins { get; set; } = [];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(TenantDomainPrefix)
        && !string.IsNullOrWhiteSpace(ApiClientId);

    public string Authority =>
        $"https://{TenantDomainPrefix.Trim()}.ciamlogin.com/{TenantId.Trim()}/v2.0";
}

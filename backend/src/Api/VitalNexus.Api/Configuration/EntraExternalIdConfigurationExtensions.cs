using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.Api.Configuration;

public static class EntraExternalIdConfigurationExtensions
{
    public static EntraExternalIdOptions BindEntraExternalIdOptions(this IConfiguration configuration)
    {
        var options = configuration.GetSection(EntraExternalIdOptions.SectionName).Get<EntraExternalIdOptions>()
            ?? new EntraExternalIdOptions();

        options.TenantId = FirstNonEmpty(options.TenantId, configuration["B2C_TENANT_ID"], Environment.GetEnvironmentVariable("B2C_TENANT_ID"));
        options.ApiClientId = FirstNonEmpty(options.ApiClientId, configuration["B2C_API_CLIENT_ID"], Environment.GetEnvironmentVariable("B2C_API_CLIENT_ID"));
        options.TenantDomainPrefix = FirstNonEmpty(
            options.TenantDomainPrefix,
            configuration["B2C_TENANT_DOMAIN_PREFIX"],
            Environment.GetEnvironmentVariable("B2C_TENANT_DOMAIN_PREFIX"));

        return options;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}

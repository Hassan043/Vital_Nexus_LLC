using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.Api.Configuration;

public static class EntraExternalIdConfigurationExtensions
{
    public static EntraExternalIdOptions BindEntraExternalIdOptions(this IConfiguration configuration)
    {
        var options = configuration.GetSection(EntraExternalIdOptions.SectionName).Get<EntraExternalIdOptions>()
            ?? new EntraExternalIdOptions();

        options.TenantId = FirstNonEmpty(
            options.TenantId,
            configuration["B2C_TENANT_ID"],
            Environment.GetEnvironmentVariable("B2C_TENANT_ID"));
        options.ApiClientId = FirstNonEmpty(
            options.ApiClientId,
            configuration["B2C_API_CLIENT_ID"],
            Environment.GetEnvironmentVariable("B2C_API_CLIENT_ID"));
        options.TenantDomainPrefix = FirstNonEmpty(
            options.TenantDomainPrefix,
            configuration["B2C_TENANT_DOMAIN_PREFIX"],
            Environment.GetEnvironmentVariable("B2C_TENANT_DOMAIN_PREFIX"));
        options.ApplicationIdUri = FirstNonEmpty(
            options.ApplicationIdUri,
            configuration["B2C_API_APPLICATION_ID_URI"],
            Environment.GetEnvironmentVariable("B2C_API_APPLICATION_ID_URI"));
        options.UserFlowId = FirstNonEmpty(
            options.UserFlowId,
            configuration["B2C_USER_FLOW"],
            Environment.GetEnvironmentVariable("B2C_USER_FLOW"));

        var tenantKind = FirstNonEmpty(
            configuration[$"{EntraExternalIdOptions.SectionName}:TenantKind"],
            configuration["B2C_TENANT_KIND"],
            Environment.GetEnvironmentVariable("B2C_TENANT_KIND"));

        if (!string.IsNullOrWhiteSpace(tenantKind))
        {
            options.TenantKind = tenantKind.Equals("b2c", StringComparison.OrdinalIgnoreCase)
                ? EntraExternalIdTenantKind.B2c
                : EntraExternalIdTenantKind.Ciam;
        }

        return options;
    }

    public static void EnsureEntraExternalIdConfiguredForEnvironment(
        this EntraExternalIdOptions options,
        IHostEnvironment environment)
    {
        if (options.IsConfigured || environment.IsDevelopment())
        {
            return;
        }

        throw new InvalidOperationException(
            "Entra External ID JWT validation is required outside Development. " +
            "Configure EntraExternalId (TenantId, TenantDomainPrefix, ApiClientId) or " +
            "set B2C_TENANT_ID, B2C_TENANT_DOMAIN_PREFIX, and B2C_API_CLIENT_ID.");
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

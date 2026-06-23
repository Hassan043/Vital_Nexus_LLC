using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.UnitTests.Configuration;

public sealed class EntraExternalIdOptionsTests
{
    [Fact]
    public void BuildAuthority_UsesCiamMetadataEndpointWithV2Suffix()
    {
        var options = new EntraExternalIdOptions
        {
            TenantKind = EntraExternalIdTenantKind.Ciam,
            TenantId = "00000000-0000-4000-8000-000000000001",
            TenantDomainPrefix = "vitalnexusexternal",
            ApiClientId = "api-client-id",
        };

        Assert.Equal(
            "https://vitalnexusexternal.ciamlogin.com/00000000-0000-4000-8000-000000000001/v2.0",
            options.BuildAuthority());
    }

    [Fact]
    public void BuildAuthority_UsesLegacyB2cUserFlowMetadataEndpoint()
    {
        var options = new EntraExternalIdOptions
        {
            TenantKind = EntraExternalIdTenantKind.B2c,
            TenantId = "00000000-0000-4000-8000-000000000001",
            TenantDomainPrefix = "vitalnexusdev",
            UserFlowId = "B2C_1_VitalNexusSignUpSignIn",
            ApiClientId = "api-client-id",
        };

        Assert.Equal(
            "https://vitalnexusdev.b2clogin.com/vitalnexusdev.onmicrosoft.com/B2C_1_VitalNexusSignUpSignIn/v2.0",
            options.BuildAuthority());
    }

    [Fact]
    public void GetValidAudiences_IncludesApiClientIdAndApplicationIdUri()
    {
        var options = new EntraExternalIdOptions
        {
            ApiClientId = "11111111-1111-1111-1111-111111111111",
            ApplicationIdUri = "https://vitalnexusexternal.onmicrosoft.com/vitalnexus-api",
        };

        var audiences = options.GetValidAudiences();

        Assert.Equal(2, audiences.Count);
        Assert.Contains("11111111-1111-1111-1111-111111111111", audiences);
        Assert.Contains("https://vitalnexusexternal.onmicrosoft.com/vitalnexus-api", audiences);
    }

    [Theory]
    [InlineData("", "prefix", "client")]
    [InlineData("tenant", "", "client")]
    [InlineData("tenant", "prefix", "")]
    public void IsConfigured_IsFalseWhenRequiredValuesAreMissing(string tenantId, string prefix, string clientId)
    {
        var options = new EntraExternalIdOptions
        {
            TenantId = tenantId,
            TenantDomainPrefix = prefix,
            ApiClientId = clientId,
        };

        Assert.False(options.IsConfigured);
    }
}

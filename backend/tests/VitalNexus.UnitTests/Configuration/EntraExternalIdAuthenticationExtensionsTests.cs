using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.UnitTests.Configuration;

public sealed class EntraExternalIdAuthenticationExtensionsTests
{
    [Fact]
    public void AddEntraExternalIdAuthentication_ConfiguresJwtBearerValidationParameters()
    {
        var options = CreateConfiguredOptions();
        var jwtOptions = BuildJwtBearerOptions(options);

        Assert.Equal(options.Authority, jwtOptions.Authority);
        Assert.False(jwtOptions.MapInboundClaims);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuer);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateAudience);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateLifetime);
        Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.Equal(options.GetValidAudiences(), jwtOptions.TokenValidationParameters.ValidAudiences);
        Assert.Equal("name", jwtOptions.TokenValidationParameters.NameClaimType);
    }

    [Fact]
    public void AddEntraExternalIdAuthentication_ThrowsWhenOptionsAreIncomplete()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddEntraExternalIdAuthentication(new EntraExternalIdOptions()));

        Assert.Contains("TenantId, TenantDomainPrefix, and ApiClientId", exception.Message);
    }

    private static JwtBearerOptions BuildJwtBearerOptions(EntraExternalIdOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEntraExternalIdAuthentication(options);

        using var provider = services.BuildServiceProvider();
        return provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
    }

    private static EntraExternalIdOptions CreateConfiguredOptions()
    {
        return new EntraExternalIdOptions
        {
            TenantKind = EntraExternalIdTenantKind.Ciam,
            TenantId = "00000000-0000-4000-8000-000000000001",
            TenantDomainPrefix = "vitalnexusexternal",
            ApiClientId = "11111111-1111-1111-1111-111111111111",
            ApplicationIdUri = "https://vitalnexusexternal.onmicrosoft.com/vitalnexus-api",
            RequiredScope = "access_as_user",
        };
    }
}

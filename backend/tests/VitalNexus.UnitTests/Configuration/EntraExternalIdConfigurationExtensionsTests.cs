using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VitalNexus.Api.Configuration;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.UnitTests.Configuration;

public sealed class EntraExternalIdConfigurationExtensionsTests
{
    [Fact]
    public void BindEntraExternalIdOptions_ReadsEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("B2C_TENANT_ID", "00000000-0000-4000-8000-000000000099");
        Environment.SetEnvironmentVariable("B2C_API_CLIENT_ID", "api-client-id");
        Environment.SetEnvironmentVariable("B2C_TENANT_DOMAIN_PREFIX", "vitalnexusexternal");
        Environment.SetEnvironmentVariable("B2C_API_APPLICATION_ID_URI", "https://example.onmicrosoft.com/vitalnexus-api");
        Environment.SetEnvironmentVariable("B2C_TENANT_KIND", "ciam");

        try
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var options = configuration.BindEntraExternalIdOptions();

            Assert.Equal("00000000-0000-4000-8000-000000000099", options.TenantId);
            Assert.Equal("api-client-id", options.ApiClientId);
            Assert.Equal("vitalnexusexternal", options.TenantDomainPrefix);
            Assert.Equal("https://example.onmicrosoft.com/vitalnexus-api", options.ApplicationIdUri);
            Assert.Equal(EntraExternalIdTenantKind.Ciam, options.TenantKind);
            Assert.True(options.IsConfigured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("B2C_TENANT_ID", null);
            Environment.SetEnvironmentVariable("B2C_API_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("B2C_TENANT_DOMAIN_PREFIX", null);
            Environment.SetEnvironmentVariable("B2C_API_APPLICATION_ID_URI", null);
            Environment.SetEnvironmentVariable("B2C_TENANT_KIND", null);
        }
    }

    [Fact]
    public void EnsureEntraExternalIdConfiguredForEnvironment_AllowsMissingConfigInDevelopment()
    {
        var options = new EntraExternalIdOptions();
        var environment = new TestHostEnvironment("Development");

        var exception = Record.Exception(() => options.EnsureEntraExternalIdConfiguredForEnvironment(environment));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureEntraExternalIdConfiguredForEnvironment_ThrowsOutsideDevelopmentWhenMissingConfig()
    {
        var options = new EntraExternalIdOptions();
        var environment = new TestHostEnvironment("Production");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            options.EnsureEntraExternalIdConfiguredForEnvironment(environment));

        Assert.Contains("Entra External ID JWT validation is required outside Development", exception.Message);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "VitalNexus.Api";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}

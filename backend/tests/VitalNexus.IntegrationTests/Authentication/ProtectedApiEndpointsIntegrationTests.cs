using System.Net;
using System.Net.Http.Headers;
using VitalNexus.IntegrationTests.Support;

namespace VitalNexus.IntegrationTests.Authentication;

[Collection(EntraExternalIdTestCollection.Name)]
public sealed class ProtectedApiEndpointsIntegrationTests
{
    private readonly EntraExternalIdWebApplicationFactory _factory;

    public ProtectedApiEndpointsIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task BusinessEndpoints_WithoutBearerToken_ReturnUnauthorized(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task BusinessEndpoints_WithMissingScope_ReturnForbidden(string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateAccessToken(includeRequiredScope: false));

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accounts_WithValidAccessToken_ReturnsCurrentAccountSummary()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateAccessToken(includeRequiredScope: true));

        var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("clinician@example.com", payload, StringComparison.Ordinal);
        Assert.Contains("00000000-0000-4000-8000-000000000099", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Provider_WithValidAccessToken_ReturnsCurrentProviderSummary()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateAccessToken(includeRequiredScope: true));

        var response = await client.GetAsync("/api/provider");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("pending", payload, StringComparison.Ordinal);
        Assert.Contains("clinician@example.com", payload, StringComparison.Ordinal);
    }
}

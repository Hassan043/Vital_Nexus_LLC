using System.Net;
using System.Net.Http.Headers;
using VitalNexus.IntegrationTests.Authentication;
using VitalNexus.IntegrationTests.Support;

namespace VitalNexus.IntegrationTests.Authentication;

[Collection(EntraExternalIdTestCollection.Name)]
public sealed class EntraExternalIdTokenValidationIntegrationTests
{
    private readonly EntraExternalIdWebApplicationFactory _factory;

    public EntraExternalIdTokenValidationIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsExpiredAccessToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Expired().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsTokenWithInvalidSignature()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.WithWrongSigningKey().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsTokenWithInvalidIssuer()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.WithWrongIssuer().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsTokenWithInvalidAudience()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.WithWrongAudience().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsMalformedBearerToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            "not-a-valid-jwt");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AcceptsTokenWithApplicationIdUriAudience()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.WithApplicationIdUriAudience().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsValidTokenWithoutRequiredScope()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.WithoutRequiredScope().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

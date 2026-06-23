using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using VitalNexus.IntegrationTests.Support;

namespace VitalNexus.IntegrationTests.Authentication;

[Collection(EntraExternalIdTestCollection.Name)]
public sealed class AccountsUserMappingIntegrationTests
{
    private readonly EntraExternalIdWebApplicationFactory _factory;

    public AccountsUserMappingIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_ReturnsMappedInternalAccountsUserId()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid().Build());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("userId", out var userId));
        Assert.True(Guid.TryParse(userId.GetString(), out _));
        Assert.Equal("00000000-0000-4000-8000-000000000099", root.GetProperty("entraObjectId").GetString());
        Assert.Equal("clinician@example.com", root.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Accounts_ReusesSameInternalUserIdForRepeatRequests()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid().Build());

        var firstResponse = await client.GetAsync("/api/accounts");
        var secondResponse = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var firstDocument = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        using var secondDocument = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            firstDocument.RootElement.GetProperty("userId").GetString(),
            secondDocument.RootElement.GetProperty("userId").GetString());
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;
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

        var roles = root.GetProperty("roles");
        Assert.Equal(JsonValueKind.Array, roles.ValueKind);
        Assert.Contains(roles.EnumerateArray(), role => role.GetString() == "Admin");

        var clinicMemberships = root.GetProperty("clinicMemberships");
        Assert.Equal(JsonValueKind.Array, clinicMemberships.ValueKind);
        Assert.NotEmpty(clinicMemberships.EnumerateArray());
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

    [Fact]
    public async Task Provider_ReturnsCompleteOnboardingWhenUserHasActiveClinicMembership()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4000-8000-000000000077")
                .Build());

        var providerResponse = await client.GetAsync("/api/provider");
        Assert.Equal(HttpStatusCode.OK, providerResponse.StatusCode);

        using var providerDocument = JsonDocument.Parse(await providerResponse.Content.ReadAsStringAsync());
        var root = providerDocument.RootElement;

        Assert.Equal("complete", root.GetProperty("onboardingStatus").GetString());
        Assert.Equal(JsonValueKind.Array, root.GetProperty("roles").ValueKind);
        Assert.NotEmpty(root.GetProperty("clinicMemberships").EnumerateArray());
    }
}

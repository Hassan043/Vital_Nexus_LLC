using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;
using VitalNexus.IntegrationTests.Support;

namespace VitalNexus.IntegrationTests.Authentication;

[Collection(EntraExternalIdTestCollection.Name)]
public sealed class RbacBoundaryIntegrationTests
{
    private readonly EntraExternalIdWebApplicationFactory _factory;

    public RbacBoundaryIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task CustomerEndpoints_WithDefaultAdminRole_ReturnOk(string path)
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000010");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task CustomerEndpoints_WithUserRoleOnly_ReturnOk(string path)
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000011");
        var provision = await ProvisionUserAsync(client);

        var roleRepository = GetRoleRepository();
        await roleRepository.RemoveAllRolesForUserAsync(provision.UserId);
        await roleRepository.AssignRoleAsync(provision.UserId, provision.CustomerId, ApplicationRoles.User);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task CustomerEndpoints_WithBothCustomerRoles_ReturnOk(string path)
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000012");
        var provision = await ProvisionUserAsync(client);

        var roleRepository = GetRoleRepository();
        await roleRepository.AssignRoleAsync(provision.UserId, provision.CustomerId, ApplicationRoles.User);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/me", "00000000-0000-4004-8000-000000000131")]
    [InlineData("/api/accounts", "00000000-0000-4004-8000-000000000132")]
    [InlineData("/api/provider", "00000000-0000-4004-8000-000000000133")]
    public async Task CustomerEndpoints_WithValidScopeButNoApplicationRoles_ReturnForbidden(
        string path,
        string objectId)
    {
        using var client = CreateAuthenticatedClient(objectId);
        var provision = await ProvisionUserAsync(client);

        await GetRoleRepository().RemoveAllRolesForUserAsync(provision.UserId);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task CustomerEndpoints_WithApplicationRoleButMissingScope_ReturnForbidden(string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.WithoutRequiredScope()
                .WithObjectId("00000000-0000-4004-8000-000000000014")
                .Build());

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CustomerEndpoints_AfterRolesRemovedOnSubsequentRequest_ReturnForbidden()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000015");
        var provision = await ProvisionUserAsync(client);

        var firstResponse = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        await GetRoleRepository().RemoveAllRolesForUserAsync(provision.UserId);

        var secondResponse = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Forbidden, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Accounts_WithAdminRole_IncludesRoleInResponse()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000016");

        var response = await client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var roles = document.RootElement.GetProperty("roles");

        Assert.Contains(
            roles.EnumerateArray(),
            role => role.GetString() == ApplicationRoles.Admin);
        Assert.DoesNotContain(
            roles.EnumerateArray(),
            role => role.GetString() == ApplicationRoles.User);
    }

    [Fact]
    public async Task AssignRoleAsync_RejectsSecondAdminForSameCustomer()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000017");
        var firstProvision = await ProvisionUserAsync(client);

        var secondClient = CreateAuthenticatedClient("00000000-0000-4004-8000-000000000018");
        var secondProvision = await ProvisionUserAsync(secondClient);

        var roleRepository = GetRoleRepository();
        await roleRepository.RemoveAllRolesForUserAsync(secondProvision.UserId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            roleRepository.AssignRoleAsync(
                secondProvision.UserId,
                firstProvision.CustomerId,
                ApplicationRoles.Admin));

        Assert.Contains("one Admin", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthenticatedClient(string objectId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid().WithObjectId(objectId).Build());
        return client;
    }

    private static async Task<(Guid UserId, Guid CustomerId)> ProvisionUserAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            Guid.Parse(document.RootElement.GetProperty("userId").GetString()!),
            Guid.Parse(document.RootElement.GetProperty("customerId").GetString()!));
    }

    private InMemoryUserRoleRepository GetRoleRepository() =>
        (InMemoryUserRoleRepository)_factory.Services.GetRequiredService<IUserRoleRepository>();
}

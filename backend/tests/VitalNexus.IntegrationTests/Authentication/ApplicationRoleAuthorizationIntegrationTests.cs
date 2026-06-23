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
public sealed class ApplicationRoleAuthorizationIntegrationTests
{
    private readonly EntraExternalIdWebApplicationFactory _factory;

    public ApplicationRoleAuthorizationIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CustomerEndpoints_WithValidTokenButNoApplicationRoles_ReturnForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000001")
                .Build());

        var provision = await ProvisionUserAsync(client);
        await GetRoleRepository().RemoveAllRolesForUserAsync(provision.UserId);

        foreach (var path in new[] { "/api/me", "/api/accounts", "/api/provider" })
        {
            var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/accounts")]
    [InlineData("/api/provider")]
    public async Task CustomerEndpoints_WithAdminRole_ReturnOk(string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000002")
                .WithEmail("admin@example.com")
                .Build());

        var provision = await ProvisionUserAsync(client);
        var roleRepository = GetRoleRepository();
        await roleRepository.RemoveAllRolesForUserAsync(provision.UserId);
        await roleRepository.AssignRoleAsync(provision.UserId, provision.CustomerId, ApplicationRoles.Admin);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Accounts_WithDefaultAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000003")
                .Build());

        var response = await client.GetAsync("/api/accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var roles = document.RootElement.GetProperty("roles");
        Assert.Contains(
            roles.EnumerateArray(),
            role => role.GetString() == ApplicationRoles.Admin);
    }

    [Fact]
    public async Task AdminAccountEndpoint_WithUserRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000004")
                .Build());

        var provision = await ProvisionUserAsync(client);
        var roleRepository = GetRoleRepository();
        await roleRepository.RemoveAllRolesForUserAsync(provision.UserId);
        await roleRepository.AssignRoleAsync(provision.UserId, provision.CustomerId, ApplicationRoles.User);

        var response = await client.GetAsync("/api/admin/account");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminAccountEndpoint_WithAdminRole_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000005")
                .Build());

        var response = await client.GetAsync("/api/admin/account");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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

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
    public async Task ProviderEndpoints_WithValidTokenButNoApplicationRoles_ReturnForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000001")
                .Build());

        var provisionResponse = await client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, provisionResponse.StatusCode);

        using var provisionDocument = JsonDocument.Parse(await provisionResponse.Content.ReadAsStringAsync());
        var userId = Guid.Parse(provisionDocument.RootElement.GetProperty("userId").GetString()!);

        var roleRepository = (InMemoryUserRoleRepository)_factory.Services
            .GetRequiredService<IUserRoleRepository>();
        await roleRepository.RemoveAllRolesForUserAsync(userId);

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
    public async Task ProviderEndpoints_WithClinicAdminRole_ReturnOk(string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4004-8000-000000000002")
                .WithEmail("admin@example.com")
                .Build());

        var provisionResponse = await client.GetAsync("/api/accounts");
        Assert.Equal(HttpStatusCode.OK, provisionResponse.StatusCode);

        using var provisionDocument = JsonDocument.Parse(await provisionResponse.Content.ReadAsStringAsync());
        var userId = Guid.Parse(provisionDocument.RootElement.GetProperty("userId").GetString()!);

        var roleRepository = (InMemoryUserRoleRepository)_factory.Services
            .GetRequiredService<IUserRoleRepository>();
        await roleRepository.RemoveAllRolesForUserAsync(userId);
        await roleRepository.AssignRoleAsync(userId, ApplicationRoles.ClinicAdmin);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Accounts_WithDefaultClinicianRole_ReturnsOk()
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
            role => role.GetString() == ApplicationRoles.Clinician);
    }
}

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
public sealed class TenantBoundaryIntegrationTests
{
    private static readonly Guid ClinicAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ClinicBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ClinicCId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly EntraExternalIdWebApplicationFactory _factory;

    public TenantBoundaryIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_WithUnauthorizedClinicHeader_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000021");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-CustomerA");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicBId.ToString());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("activeClinic").ValueKind);
    }

    [Fact]
    public async Task Provider_WithUnauthorizedClinicHeader_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000022");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-CustomerA");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicCId.ToString());

        var response = await client.GetAsync("/api/provider");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("activeClinic").ValueKind);
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/provider")]
    public async Task Endpoints_WithMultipleMembershipsAndNoHeader_ReturnNullActiveClinic(string path)
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000023");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-CustomerA");
        await SeedMembershipAndRoutingAsync(userId, ClinicBId, "Clinic B", "Patients-CustomerA");

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("activeClinic").ValueKind);
    }

    [Fact]
    public async Task Me_CrossTenantClinicHeader_DoesNotExposeOtherClinicDatabase()
    {
        using var clientA = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000024");
        var userAId = await GetUserIdAsync(clientA);
        await SeedMembershipAndRoutingAsync(userAId, ClinicAId, "Clinic A", "Patients-CustomerA");

        using var clientB = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000025");
        var userBId = await GetUserIdAsync(clientB);
        await SeedMembershipAndRoutingAsync(userBId, ClinicBId, "Clinic B", "Patients-CustomerB");

        clientA.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicBId.ToString());

        var response = await clientA.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");
        Assert.Equal(JsonValueKind.Null, activeClinic.ValueKind);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Patients-CustomerB", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Me_SwitchingAuthorizedClinicHeader_ReturnsMatchingClinicWithSharedPatientsDatabase()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000026");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-SharedCustomer");
        await SeedMembershipAndRoutingAsync(userId, ClinicBId, "Clinic B", "Patients-SharedCustomer");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicAId.ToString());
        var responseA = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        using var documentA = JsonDocument.Parse(await responseA.Content.ReadAsStringAsync());
        Assert.Equal(ClinicAId.ToString(), documentA.RootElement.GetProperty("activeClinic").GetProperty("clinicId").GetString());
        var databaseName = documentA.RootElement.GetProperty("activeClinic").GetProperty("patientsDatabaseName").GetString();
        Assert.False(string.IsNullOrWhiteSpace(databaseName));

        client.DefaultRequestHeaders.Remove(ClinicContextHeaders.ClinicId);
        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicBId.ToString());
        var responseB = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        using var documentB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        Assert.Equal(ClinicBId.ToString(), documentB.RootElement.GetProperty("activeClinic").GetProperty("clinicId").GetString());
        Assert.Equal(
            databaseName,
            documentB.RootElement.GetProperty("activeClinic").GetProperty("patientsDatabaseName").GetString());
    }

    [Fact]
    public async Task Me_WithInactiveMembership_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000027");
        var userId = await GetUserIdAsync(client);

        var membershipRepository = GetMembershipRepository();
        var memberships = await membershipRepository.GetMembershipsForUserAsync(userId);
        foreach (var membership in memberships)
        {
            await membershipRepository.AddMembershipAsync(
                userId,
                new ClinicMembership
                {
                    ClinicId = membership.ClinicId,
                    ClinicName = membership.ClinicName,
                    JoinedAt = membership.JoinedAt,
                    IsActive = false,
                });
        }

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("activeClinic").ValueKind);
    }

    [Fact]
    public async Task Me_WithInactiveRouting_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000028");
        var userId = await GetUserIdAsync(client);

        await GetMembershipRepository().AddMembershipAsync(
            userId,
            new ClinicMembership
            {
                ClinicId = ClinicAId,
                ClinicName = "Clinic A",
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
            });

        var user = await GetUserAsync(userId);
        await GetRoutingRepository().UpsertAsync(new CustomerPatientsDatabase
        {
            CustomerId = user.CustomerId,
            DatabaseName = "Patients-CustomerA",
            IsActive = false,
            ProvisionedAt = DateTime.UtcNow,
        });

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("activeClinic").ValueKind);
    }

    [Fact]
    public async Task Me_WithMalformedClinicHeaderAndSingleMembership_UsesDefaultClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000029");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");
        Assert.NotEqual(JsonValueKind.Null, activeClinic.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(activeClinic.GetProperty("clinicId").GetString()));
    }

    [Fact]
    public async Task Me_WithMalformedClinicHeaderAndMultipleMemberships_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000030");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-CustomerA");
        await SeedMembershipAndRoutingAsync(userId, ClinicBId, "Clinic B", "Patients-CustomerA");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, "invalid");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("activeClinic").ValueKind);
    }

    [Fact]
    public async Task Me_ActiveClinicResponse_DoesNotExposePatientsConnectionString()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000031");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-CustomerA");

        var response = await client.GetAsync("/api/me");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("Initial Catalog", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PatientsConnectionString", payload, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateAuthenticatedClient(string objectId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid().WithObjectId(objectId).Build());
        return client;
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient client)
    {
        var meResponse = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        using var meDocument = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        return Guid.Parse(meDocument.RootElement.GetProperty("userId").GetString()!);
    }

    private async Task<AccountsUser> GetUserAsync(Guid userId)
    {
        var repository = (InMemoryAccountsUserRepository)_factory.Services
            .GetRequiredService<IAccountsUserRepository>();
        return (await repository.GetByIdAsync(userId))!;
    }

    private async Task SeedMembershipAndRoutingAsync(
        Guid userId,
        Guid clinicId,
        string clinicName,
        string patientsDatabaseName)
    {
        await GetMembershipRepository().AddMembershipAsync(
            userId,
            new ClinicMembership
            {
                ClinicId = clinicId,
                ClinicName = clinicName,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
            });

        await SeedCustomerRoutingAsync(userId, patientsDatabaseName);
    }

    private async Task SeedCustomerRoutingAsync(Guid userId, string patientsDatabaseName)
    {
        var user = await GetUserAsync(userId);
        var existing = await GetRoutingRepository().GetByCustomerIdAsync(user.CustomerId);
        if (existing is null)
        {
            await GetRoutingRepository().CreateAsync(new CustomerPatientsDatabase
            {
                CustomerId = user.CustomerId,
                DatabaseName = patientsDatabaseName,
                IsActive = true,
                ProvisionedAt = DateTime.UtcNow,
            });
        }
    }

    private InMemoryClinicMembershipRepository GetMembershipRepository() =>
        (InMemoryClinicMembershipRepository)_factory.Services
            .GetRequiredService<IClinicMembershipRepository>();

    private InMemoryCustomerPatientsDatabaseRepository GetRoutingRepository() =>
        (InMemoryCustomerPatientsDatabaseRepository)_factory.Services
            .GetRequiredService<ICustomerPatientsDatabaseRepository>();
}

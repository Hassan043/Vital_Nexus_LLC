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
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");

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
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");

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
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");
        await SeedMembershipAndRoutingAsync(userId, ClinicBId, "Clinic B", "Patients-ClinicB");

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
        await SeedMembershipAndRoutingAsync(userAId, ClinicAId, "Clinic A", "Patients-ClinicA");

        using var clientB = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000025");
        var userBId = await GetUserIdAsync(clientB);
        await SeedMembershipAndRoutingAsync(userBId, ClinicBId, "Clinic B", "Patients-ClinicB");

        clientA.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicBId.ToString());

        var response = await clientA.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");
        Assert.Equal(JsonValueKind.Null, activeClinic.ValueKind);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Patients-ClinicB", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Me_SwitchingAuthorizedClinicHeader_ReturnsMatchingPatientsDatabase()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000026");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");
        await SeedMembershipAndRoutingAsync(userId, ClinicBId, "Clinic B", "Patients-ClinicB");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicAId.ToString());
        var responseA = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, responseA.StatusCode);
        using var documentA = JsonDocument.Parse(await responseA.Content.ReadAsStringAsync());
        Assert.Equal(
            "Patients-ClinicA",
            documentA.RootElement.GetProperty("activeClinic").GetProperty("patientsDatabaseName").GetString());

        client.DefaultRequestHeaders.Remove(ClinicContextHeaders.ClinicId);
        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, ClinicBId.ToString());
        var responseB = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, responseB.StatusCode);
        using var documentB = JsonDocument.Parse(await responseB.Content.ReadAsStringAsync());
        Assert.Equal(
            "Patients-ClinicB",
            documentB.RootElement.GetProperty("activeClinic").GetProperty("patientsDatabaseName").GetString());
    }

    [Fact]
    public async Task Me_WithInactiveMembership_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000027");
        var userId = await GetUserIdAsync(client);

        var membershipRepository = GetMembershipRepository();
        await membershipRepository.AddMembershipAsync(
            userId,
            new ClinicMembership
            {
                ClinicId = ClinicAId,
                ClinicName = "Inactive Clinic",
                JoinedAt = DateTime.UtcNow,
                IsActive = false,
            });
        await GetRoutingRepository().AddRoutingAsync(new ClinicPatientsDatabase
        {
            ClinicId = ClinicAId,
            DatabaseName = "Patients-InactiveClinic",
            IsActive = true,
        });

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
        await GetRoutingRepository().AddRoutingAsync(new ClinicPatientsDatabase
        {
            ClinicId = ClinicAId,
            DatabaseName = "Patients-ClinicA",
            IsActive = false,
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
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, "not-a-guid");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");
        Assert.Equal(ClinicAId.ToString(), activeClinic.GetProperty("clinicId").GetString());
    }

    [Fact]
    public async Task Me_WithMalformedClinicHeaderAndMultipleMemberships_ReturnsNullActiveClinic()
    {
        using var client = CreateAuthenticatedClient("00000000-0000-4000-8000-000000000030");
        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");
        await SeedMembershipAndRoutingAsync(userId, ClinicBId, "Clinic B", "Patients-ClinicB");

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
        await SeedMembershipAndRoutingAsync(userId, ClinicAId, "Clinic A", "Patients-ClinicA");

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

        await GetRoutingRepository().AddRoutingAsync(new ClinicPatientsDatabase
        {
            ClinicId = clinicId,
            DatabaseName = patientsDatabaseName,
            IsActive = true,
        });
    }

    private InMemoryClinicMembershipRepository GetMembershipRepository() =>
        (InMemoryClinicMembershipRepository)_factory.Services
            .GetRequiredService<IClinicMembershipRepository>();

    private InMemoryClinicPatientsDatabaseRepository GetRoutingRepository() =>
        (InMemoryClinicPatientsDatabaseRepository)_factory.Services
            .GetRequiredService<IClinicPatientsDatabaseRepository>();
}

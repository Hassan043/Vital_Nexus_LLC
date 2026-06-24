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
public sealed class ClinicContextIntegrationTests
{
    private static readonly Guid TestClinicId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid SecondClinicId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly EntraExternalIdWebApplicationFactory _factory;

    public ClinicContextIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_ReturnsActiveClinicAfterCustomerOnboardingIsCompleted()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4000-8000-000000000001")
                .Build());

        await OnboardingTestHelper.CompleteDemoOnboardingAsync(client);

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");
        Assert.NotEqual(JsonValueKind.Null, activeClinic.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(activeClinic.GetProperty("patientsDatabaseName").GetString()));
    }

    [Fact]
    public async Task Me_ReturnsActiveClinicWhenUserHasSingleMembershipAndRoutingExists()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4000-8000-000000000088")
                .WithEmail("single-clinic@example.com")
                .Build());

        await OnboardingTestHelper.CompleteDemoOnboardingAsync(client);

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");

        Assert.NotEqual(JsonValueKind.Null, activeClinic.ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(activeClinic.GetProperty("clinicId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(activeClinic.GetProperty("patientsDatabaseName").GetString()));
    }

    [Fact]
    public async Task Me_UsesRequestedClinicHeaderWhenUserHasMultipleMemberships()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EntraExternalIdTestTokenBuilder.Valid()
                .WithObjectId("00000000-0000-4000-8000-000000000066")
                .Build());

        var userId = await GetUserIdAsync(client);
        await SeedMembershipAndRoutingAsync(userId, SecondClinicId, "Clinic B", "Patients-SharedCustomer");

        client.DefaultRequestHeaders.Add(ClinicContextHeaders.ClinicId, SecondClinicId.ToString());

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var activeClinic = document.RootElement.GetProperty("activeClinic");

        Assert.Equal(SecondClinicId.ToString(), activeClinic.GetProperty("clinicId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(activeClinic.GetProperty("patientsDatabaseName").GetString()));
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient client)
    {
        var meResponse = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        using var meDocument = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        return Guid.Parse(meDocument.RootElement.GetProperty("userId").GetString()!);
    }

    private async Task<Guid> GetCustomerIdAsync(HttpClient client)
    {
        var meResponse = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        using var meDocument = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        return Guid.Parse(meDocument.RootElement.GetProperty("customerId").GetString()!);
    }

    private async Task SeedMembershipAndRoutingAsync(
        Guid userId,
        Guid clinicId,
        string clinicName,
        string patientsDatabaseName)
    {
        var membershipRepository = (InMemoryClinicMembershipRepository)_factory.Services
            .GetRequiredService<IClinicMembershipRepository>();
        await membershipRepository.AddMembershipAsync(
            userId,
            new ClinicMembership
            {
                ClinicId = clinicId,
                ClinicName = clinicName,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
            });

        var accountsRepository = (InMemoryAccountsUserRepository)_factory.Services
            .GetRequiredService<IAccountsUserRepository>();
        var user = await accountsRepository.GetByIdAsync(userId);
        Assert.NotNull(user);

        var routingRepository = (InMemoryCustomerPatientsDatabaseRepository)_factory.Services
            .GetRequiredService<ICustomerPatientsDatabaseRepository>();
        var existing = await routingRepository.GetByCustomerIdAsync(user!.CustomerId);
        if (existing is null)
        {
            await routingRepository.CreateAsync(new CustomerPatientsDatabase
            {
                CustomerId = user.CustomerId,
                DatabaseName = patientsDatabaseName,
                IsActive = true,
                ProvisionedAt = DateTime.UtcNow,
            });
        }
    }
}

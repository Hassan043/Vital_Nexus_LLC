using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace VitalNexus.IntegrationTests.Support;

public static class OnboardingTestHelper
{
    public static async Task CompleteDemoOnboardingAsync(HttpClient client)
    {
        var baaResponse = await client.PostAsJsonAsync("/api/admin/onboarding/baa", new { });
        baaResponse.EnsureSuccessStatusCode();

        var planResponse = await client.PostAsJsonAsync("/api/admin/onboarding/plan", new { planTierId = 1 });
        planResponse.EnsureSuccessStatusCode();

        var profileResponse = await client.PutAsJsonAsync(
            "/api/admin/onboarding/clinic-profile",
            new
            {
                customerDisplayName = "Demo Customer",
                clinicName = "Demo Primary Clinic",
                contactEmail = "admin@demo.example.com",
                phone = "+1-555-0100",
                timeZoneId = "America/New_York",
            });
        profileResponse.EnsureSuccessStatusCode();

        var completeResponse = await client.PostAsJsonAsync(
            "/api/admin/onboarding/complete",
            new
            {
                customerDisplayName = "Demo Customer",
                clinicName = "Demo Primary Clinic",
                contactEmail = "admin@demo.example.com",
                phone = "+1-555-0100",
                timeZoneId = "America/New_York",
                planTierId = 1,
            });
        completeResponse.EnsureSuccessStatusCode();
    }

    public static async Task<Guid> GetUserIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/me");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return Guid.Parse(document.RootElement.GetProperty("userId").GetString()!);
    }

    public static void UseBearerToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}

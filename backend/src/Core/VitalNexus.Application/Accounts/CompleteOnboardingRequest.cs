using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public sealed class CompleteOnboardingRequest
{
    public string CustomerDisplayName { get; init; } = string.Empty;

    public string ClinicName { get; init; } = string.Empty;

    public string? ContactEmail { get; init; }

    public string? Phone { get; init; }

    public string? TimeZoneId { get; init; }

    public int PlanTierId { get; init; }
}

using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public static class ProviderOnboardingStatus
{
    public const string Pending = "pending";

    public const string Complete = "complete";

    public static string FromMemberships(IReadOnlyList<ClinicMembership> memberships)
    {
        return memberships.Any(membership => membership.IsActive)
            ? Complete
            : Pending;
    }
}

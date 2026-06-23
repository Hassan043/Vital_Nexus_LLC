namespace VitalNexus.Application.Accounts;

public sealed class CustomerOnboardingStatus
{
    public bool CustomerCreated { get; init; }

    public bool EntraIdentityLinked { get; init; }

    public bool SubscriptionCreated { get; init; }

    public bool PatientsDatabaseProvisioned { get; init; }

    public bool DefaultClinicCreated { get; init; }

    public bool AdminAssigned { get; init; }

    public bool IsComplete =>
        CustomerCreated
        && EntraIdentityLinked
        && SubscriptionCreated
        && PatientsDatabaseProvisioned
        && DefaultClinicCreated
        && AdminAssigned;
}

namespace VitalNexus.Application.Accounts;

public sealed class CustomerOnboardingStatus
{
    public bool CustomerCreated { get; init; }

    public bool EntraIdentityLinked { get; init; }

    public bool BaaSigned { get; init; }

    public bool PlanSelected { get; init; }

    public bool ClinicProfileComplete { get; init; }

    public bool SubscriptionCreated { get; init; }

    public bool PatientsDatabaseProvisioned { get; init; }

    public bool DefaultClinicCreated { get; init; }

    public bool AdminAssigned { get; init; }

    public bool AccountActivated { get; init; }

    public bool IsComplete =>
        CustomerCreated
        && EntraIdentityLinked
        && BaaSigned
        && PlanSelected
        && ClinicProfileComplete
        && SubscriptionCreated
        && PatientsDatabaseProvisioned
        && DefaultClinicCreated
        && AdminAssigned
        && AccountActivated;
}

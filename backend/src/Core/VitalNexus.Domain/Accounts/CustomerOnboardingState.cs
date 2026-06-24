namespace VitalNexus.Domain.Accounts;

public sealed class CustomerOnboardingState
{
    public Guid CustomerId { get; init; }

    public int? SelectedPlanTierId { get; init; }

    public bool ClinicProfileComplete { get; init; }

    public DateTime? ProvisioningCompletedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

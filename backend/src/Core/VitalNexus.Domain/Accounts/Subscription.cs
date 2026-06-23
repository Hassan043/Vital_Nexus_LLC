namespace VitalNexus.Domain.Accounts;

public sealed class Subscription
{
    public Guid CustomerId { get; init; }

    public int PlanTierId { get; init; }

    public string Status { get; init; } = SubscriptionStatuses.Pending;

    public DateTime CreatedAt { get; init; }

    public DateTime? ActivatedAt { get; init; }
}

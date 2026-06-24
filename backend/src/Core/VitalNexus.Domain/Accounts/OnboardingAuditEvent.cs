namespace VitalNexus.Domain.Accounts;

public sealed class OnboardingAuditEvent
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public Guid? ActorUserId { get; init; }

    public string EventType { get; init; } = string.Empty;

    public string? Detail { get; init; }

    public DateTime OccurredAt { get; init; }
}

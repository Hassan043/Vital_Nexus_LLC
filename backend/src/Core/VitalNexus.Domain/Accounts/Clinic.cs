namespace VitalNexus.Domain.Accounts;

public sealed class Clinic
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public bool IsActive { get; init; } = true;
}

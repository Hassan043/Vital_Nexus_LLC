namespace VitalNexus.Domain.Accounts;

public sealed class Customer
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}

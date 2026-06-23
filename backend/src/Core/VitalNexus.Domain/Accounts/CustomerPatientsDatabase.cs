namespace VitalNexus.Domain.Accounts;

public sealed class CustomerPatientsDatabase
{
    public Guid CustomerId { get; init; }

    public string DatabaseName { get; init; } = string.Empty;

    public string? ServerName { get; init; }

    public bool IsActive { get; init; } = true;

    public DateTime ProvisionedAt { get; init; }
}

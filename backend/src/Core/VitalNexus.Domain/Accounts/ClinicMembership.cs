namespace VitalNexus.Domain.Accounts;

public sealed class ClinicMembership
{
    public Guid ClinicId { get; init; }

    public string ClinicName { get; init; } = string.Empty;

    public DateTime JoinedAt { get; init; }

    public bool IsActive { get; init; }
}

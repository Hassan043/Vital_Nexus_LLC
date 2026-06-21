namespace VitalNexus.Domain.Accounts;

public sealed class ClinicPatientsDatabase
{
    public Guid ClinicId { get; init; }

    public string DatabaseName { get; init; } = string.Empty;

    public string? ServerName { get; init; }

    public bool IsActive { get; init; }
}

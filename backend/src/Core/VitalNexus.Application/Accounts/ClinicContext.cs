namespace VitalNexus.Application.Accounts;

public sealed class ClinicContext
{
    public Guid ClinicId { get; init; }

    public string ClinicName { get; init; } = string.Empty;

    public string PatientsDatabaseName { get; init; } = string.Empty;

    public string PatientsConnectionString { get; init; } = string.Empty;
}

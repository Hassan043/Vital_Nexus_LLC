namespace VitalNexus.Infrastructure.Accounts;

public sealed class ClinicPatientsDatabaseOptions
{
    public const string SectionName = "ClinicPatientsDatabaseRouting";

    public string TemplateConnectionStringName { get; set; } = "PatientHealth";

    public Dictionary<string, ClinicPatientsDatabaseEntry> Clinics { get; set; } = new();
}

public sealed class ClinicPatientsDatabaseEntry
{
    public string DatabaseName { get; set; } = "PatientHealth";

    public string? ServerName { get; set; }

    public bool IsActive { get; set; } = true;
}

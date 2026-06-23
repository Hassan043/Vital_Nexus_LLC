namespace VitalNexus.Infrastructure.Accounts;

public sealed class CustomerPatientsDatabaseOptions
{
    public const string SectionName = "CustomerPatientsDatabaseRouting";

    public string TemplateConnectionStringName { get; set; } = "PatientHealth";

    public string? DefaultServerName { get; set; }
}

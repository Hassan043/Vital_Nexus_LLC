namespace VitalNexus.Domain.Accounts;

public sealed class ClinicProfile
{
    public Guid ClinicId { get; init; }

    public string? DisplayName { get; init; }

    public string? ContactEmail { get; init; }

    public string? Phone { get; init; }

    public string? TimeZoneId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}

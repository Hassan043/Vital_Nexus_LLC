namespace VitalNexus.Contracts.Contracts;

public sealed class HealthStatusDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

namespace VitalNexus.Domain.Accounts;

public sealed class PlanTier
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public int MonthlyPriceCents { get; init; }

    public int PatientCapMax { get; init; }

    public bool IsActive { get; init; } = true;
}

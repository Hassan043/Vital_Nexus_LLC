using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryPlanTierRepository : IPlanTierRepository
{
    private readonly ConcurrentDictionary<int, PlanTier> _planTiers = new(new[]
    {
        new KeyValuePair<int, PlanTier>(1, new PlanTier
        {
            Id = 1,
            Name = "Starter",
            Description = "Demo starter plan for new customers.",
            MonthlyPriceCents = 9900,
            PatientCapMax = 250,
            IsActive = true,
        }),
        new KeyValuePair<int, PlanTier>(2, new PlanTier
        {
            Id = 2,
            Name = "Professional",
            Description = "Demo professional plan with multiple clinics.",
            MonthlyPriceCents = 24900,
            PatientCapMax = 1000,
            IsActive = true,
        }),
    });

    public Task<PlanTier?> GetByIdAsync(int planTierId, CancellationToken cancellationToken = default)
    {
        _planTiers.TryGetValue(planTierId, out var tier);
        return Task.FromResult(tier);
    }

    public Task<IReadOnlyList<PlanTier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var tiers = _planTiers.Values.Where(plan => plan.IsActive).OrderBy(plan => plan.Id).ToList();
        return Task.FromResult<IReadOnlyList<PlanTier>>(tiers);
    }

    public Task<PlanTier> UpsertAsync(PlanTier planTier, CancellationToken cancellationToken = default)
    {
        _planTiers[planTier.Id] = planTier;
        return Task.FromResult(planTier);
    }
}

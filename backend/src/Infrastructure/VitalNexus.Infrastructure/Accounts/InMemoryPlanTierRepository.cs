using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryPlanTierRepository : IPlanTierRepository
{
    private static readonly IReadOnlyList<PlanTier> SeedTiers =
    [
        new PlanTier { Id = 1, Name = "Starter", Description = "Demo starter plan for new customers.", IsActive = true },
        new PlanTier { Id = 2, Name = "Professional", Description = "Demo professional plan with multiple clinics.", IsActive = true },
    ];

    public Task<PlanTier?> GetByIdAsync(int planTierId, CancellationToken cancellationToken = default)
    {
        var tier = SeedTiers.FirstOrDefault(plan => plan.Id == planTierId);
        return Task.FromResult(tier);
    }

    public Task<IReadOnlyList<PlanTier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SeedTiers);
    }
}

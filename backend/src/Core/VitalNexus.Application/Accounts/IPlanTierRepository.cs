using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IPlanTierRepository
{
    Task<PlanTier?> GetByIdAsync(int planTierId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanTier>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<PlanTier> UpsertAsync(PlanTier planTier, CancellationToken cancellationToken = default);
}

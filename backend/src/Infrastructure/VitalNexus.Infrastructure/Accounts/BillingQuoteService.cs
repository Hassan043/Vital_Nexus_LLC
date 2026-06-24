using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class BillingQuoteService(IPlanTierRepository planTierRepository) : IBillingQuoteService
{
    public async Task<BillingQuote> CreateQuoteAsync(
        int planTierId,
        int? clientPriceCents,
        CancellationToken cancellationToken = default)
    {
        if (clientPriceCents.HasValue)
        {
            throw new InvalidOperationException("Client-supplied pricing is not accepted. Use planTierId only.");
        }

        var planTier = await planTierRepository.GetByIdAsync(planTierId, cancellationToken)
            ?? throw new InvalidOperationException("Plan tier was not found.");

        if (!planTier.IsActive)
        {
            throw new InvalidOperationException("Plan tier is not active.");
        }

        return new BillingQuote
        {
            PlanTierId = planTier.Id,
            PlanName = planTier.Name,
            MonthlyPriceCents = planTier.MonthlyPriceCents,
            PatientCapMax = planTier.PatientCapMax,
        };
    }
}

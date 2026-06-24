using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IBillingQuoteService
{
    Task<BillingQuote> CreateQuoteAsync(int planTierId, int? clientPriceCents, CancellationToken cancellationToken = default);
}

public sealed class BillingQuote
{
    public int PlanTierId { get; init; }

    public string PlanName { get; init; } = string.Empty;

    public int MonthlyPriceCents { get; init; }

    public int PatientCapMax { get; init; }

    public string Currency { get; init; } = "USD";
}

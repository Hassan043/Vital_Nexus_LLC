using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryBaaAgreementRepository : IBaaAgreementRepository
{
    private readonly ConcurrentDictionary<Guid, BaaAgreement> _agreements = new();

    public Task<BaaAgreement?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _agreements.TryGetValue(customerId, out var agreement);
        return Task.FromResult(agreement);
    }

    public Task<BaaAgreement> SignAsync(BaaAgreement agreement, CancellationToken cancellationToken = default)
    {
        _agreements[agreement.CustomerId] = agreement;
        return Task.FromResult(agreement);
    }
}

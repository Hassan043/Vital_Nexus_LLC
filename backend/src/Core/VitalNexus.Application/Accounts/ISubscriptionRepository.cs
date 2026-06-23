using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface ISubscriptionRepository
{
    Task<Subscription> CreateAsync(Subscription subscription, CancellationToken cancellationToken = default);

    Task<Subscription?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}

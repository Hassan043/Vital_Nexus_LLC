using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemorySubscriptionRepository : ISubscriptionRepository
{
    private readonly ConcurrentDictionary<Guid, Subscription> _subscriptions = new();

    public Task<Subscription> CreateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        if (!_subscriptions.TryAdd(subscription.CustomerId, subscription))
        {
            throw new InvalidOperationException("A subscription already exists for this customer.");
        }

        return Task.FromResult(subscription);
    }

    public Task<Subscription?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _subscriptions.TryGetValue(customerId, out var subscription);
        return Task.FromResult(subscription);
    }
}

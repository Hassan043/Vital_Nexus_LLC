using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryCustomerOnboardingStateRepository : ICustomerOnboardingStateRepository
{
    private readonly ConcurrentDictionary<Guid, CustomerOnboardingState> _states = new();

    public Task<CustomerOnboardingState?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _states.TryGetValue(customerId, out var state);
        return Task.FromResult(state);
    }

    public Task<CustomerOnboardingState> UpsertAsync(
        CustomerOnboardingState state,
        CancellationToken cancellationToken = default)
    {
        _states[state.CustomerId] = state;
        return Task.FromResult(state);
    }
}

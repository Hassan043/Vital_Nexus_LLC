using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    private readonly ConcurrentDictionary<Guid, Customer> _customers = new();

    public Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        if (!_customers.TryAdd(customer.Id, customer))
        {
            throw new InvalidOperationException("A customer with the same id already exists.");
        }

        return Task.FromResult(customer);
    }

    public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        _customers.TryGetValue(customerId, out var customer);
        return Task.FromResult(customer);
    }
}

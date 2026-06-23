using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryCustomerPatientsDatabaseRepository : ICustomerPatientsDatabaseRepository
{
    private readonly ConcurrentDictionary<Guid, CustomerPatientsDatabase> _routing = new();

    public Task<CustomerPatientsDatabase?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        _routing.TryGetValue(customerId, out var routing);
        return Task.FromResult(routing);
    }

    public Task<CustomerPatientsDatabase> CreateAsync(
        CustomerPatientsDatabase routing,
        CancellationToken cancellationToken = default)
    {
        if (!_routing.TryAdd(routing.CustomerId, routing))
        {
            throw new InvalidOperationException("Patients database routing already exists for this customer.");
        }

        return Task.FromResult(routing);
    }

    public Task UpsertAsync(
        CustomerPatientsDatabase routing,
        CancellationToken cancellationToken = default)
    {
        _routing[routing.CustomerId] = routing;
        return Task.CompletedTask;
    }
}

using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryClinicRepository : IClinicRepository
{
    private readonly ConcurrentDictionary<Guid, Clinic> _clinics = new();

    public Task<Clinic> CreateAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        if (!_clinics.TryAdd(clinic.Id, clinic))
        {
            throw new InvalidOperationException("A clinic with the same id already exists.");
        }

        return Task.FromResult(clinic);
    }

    public Task<Clinic?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        _clinics.TryGetValue(clinicId, out var clinic);
        return Task.FromResult(clinic);
    }

    public Task<IReadOnlyList<Clinic>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var clinics = _clinics.Values
            .Where(clinic => clinic.CustomerId == customerId)
            .OrderBy(clinic => clinic.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<Clinic>>(clinics);
    }
}

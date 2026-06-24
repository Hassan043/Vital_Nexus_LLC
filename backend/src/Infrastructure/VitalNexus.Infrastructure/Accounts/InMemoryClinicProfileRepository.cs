using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryClinicProfileRepository : IClinicProfileRepository
{
    private readonly ConcurrentDictionary<Guid, ClinicProfile> _profiles = new();

    public Task<ClinicProfile> CreateAsync(ClinicProfile profile, CancellationToken cancellationToken = default)
    {
        _profiles[profile.ClinicId] = profile;
        return Task.FromResult(profile);
    }

    public Task<ClinicProfile?> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        _profiles.TryGetValue(clinicId, out var profile);
        return Task.FromResult(profile);
    }

    public Task<ClinicProfile> UpdateAsync(ClinicProfile profile, CancellationToken cancellationToken = default)
    {
        _profiles[profile.ClinicId] = profile;
        return Task.FromResult(profile);
    }
}

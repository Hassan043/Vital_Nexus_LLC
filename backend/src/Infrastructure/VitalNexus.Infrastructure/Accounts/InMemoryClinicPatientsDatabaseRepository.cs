using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryClinicPatientsDatabaseRepository : IClinicPatientsDatabaseRepository
{
    private readonly ConcurrentDictionary<Guid, ClinicPatientsDatabase> _routing = new();

    public InMemoryClinicPatientsDatabaseRepository(IOptions<ClinicPatientsDatabaseOptions> options)
    {
        foreach (var (clinicIdValue, entry) in options.Value.Clinics)
        {
            if (!Guid.TryParse(clinicIdValue, out var clinicId))
            {
                continue;
            }

            _routing[clinicId] = new ClinicPatientsDatabase
            {
                ClinicId = clinicId,
                DatabaseName = entry.DatabaseName,
                ServerName = entry.ServerName,
                IsActive = entry.IsActive,
            };
        }
    }

    public Task<ClinicPatientsDatabase?> GetByClinicIdAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        _routing.TryGetValue(clinicId, out var routing);
        return Task.FromResult(routing);
    }

    public Task AddRoutingAsync(
        ClinicPatientsDatabase routing,
        CancellationToken cancellationToken = default)
    {
        _routing[routing.ClinicId] = routing;
        return Task.CompletedTask;
    }
}

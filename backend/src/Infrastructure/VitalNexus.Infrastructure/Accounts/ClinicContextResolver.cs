using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class ClinicContextResolver(
    IClinicPatientsDatabaseRepository clinicPatientsDatabaseRepository,
    PatientsDatabaseConnectionStringFactory connectionStringFactory) : IClinicContextResolver
{
    public async Task<ClinicContext?> ResolveAsync(
        AccountsUser user,
        Guid? requestedClinicId = null,
        CancellationToken cancellationToken = default)
    {
        var activeMemberships = user.ClinicMemberships
            .Where(membership => membership.IsActive)
            .ToList();

        if (activeMemberships.Count == 0)
        {
            return null;
        }

        ClinicMembership? selectedMembership;
        if (requestedClinicId.HasValue)
        {
            selectedMembership = activeMemberships.FirstOrDefault(
                membership => membership.ClinicId == requestedClinicId.Value);
            if (selectedMembership is null)
            {
                return null;
            }
        }
        else if (activeMemberships.Count == 1)
        {
            selectedMembership = activeMemberships[0];
        }
        else
        {
            return null;
        }

        var routing = await clinicPatientsDatabaseRepository.GetByClinicIdAsync(
            selectedMembership.ClinicId,
            cancellationToken);
        if (routing is null || !routing.IsActive)
        {
            return null;
        }

        return new ClinicContext
        {
            ClinicId = selectedMembership.ClinicId,
            ClinicName = selectedMembership.ClinicName,
            PatientsDatabaseName = routing.DatabaseName,
            PatientsConnectionString = connectionStringFactory.Build(routing),
        };
    }
}

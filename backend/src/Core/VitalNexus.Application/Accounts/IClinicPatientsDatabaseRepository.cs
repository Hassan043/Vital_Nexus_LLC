using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IClinicPatientsDatabaseRepository
{
    Task<ClinicPatientsDatabase?> GetByClinicIdAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);
}

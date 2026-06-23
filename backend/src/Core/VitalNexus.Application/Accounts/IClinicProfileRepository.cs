using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IClinicProfileRepository
{
    Task<ClinicProfile> CreateAsync(ClinicProfile profile, CancellationToken cancellationToken = default);

    Task<ClinicProfile?> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellationToken = default);
}

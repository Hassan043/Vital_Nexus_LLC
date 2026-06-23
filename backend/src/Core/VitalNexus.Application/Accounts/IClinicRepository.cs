using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IClinicRepository
{
    Task<Clinic> CreateAsync(Clinic clinic, CancellationToken cancellationToken = default);

    Task<Clinic?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Clinic>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

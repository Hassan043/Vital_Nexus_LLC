using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IBaaAgreementRepository
{
    Task<BaaAgreement?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<BaaAgreement> SignAsync(BaaAgreement agreement, CancellationToken cancellationToken = default);
}

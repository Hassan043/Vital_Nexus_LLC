using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IOnboardingAuditRepository
{
    Task RecordAsync(OnboardingAuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OnboardingAuditEvent>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface ICustomerOnboardingStateRepository
{
    Task<CustomerOnboardingState?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<CustomerOnboardingState> UpsertAsync(
        CustomerOnboardingState state,
        CancellationToken cancellationToken = default);
}

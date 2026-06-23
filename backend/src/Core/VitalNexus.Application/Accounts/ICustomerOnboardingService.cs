namespace VitalNexus.Application.Accounts;

public interface ICustomerOnboardingService
{
    Task<CustomerOnboardingStatus> GetStatusAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingStatus> CompleteOnboardingAsync(
        Guid customerId,
        Guid adminUserId,
        string customerDisplayName,
        CancellationToken cancellationToken = default);
}

namespace VitalNexus.Application.Accounts;

public interface ICustomerOnboardingService
{
    Task<CustomerOnboardingStatus> GetStatusAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingStatus> SignBaaAsync(
        Guid customerId,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingStatus> SelectPlanAsync(
        Guid customerId,
        Guid adminUserId,
        int planTierId,
        int? clientPriceCents,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingStatus> UpdateClinicProfileAsync(
        Guid customerId,
        Guid adminUserId,
        CompleteOnboardingRequest profileRequest,
        CancellationToken cancellationToken = default);

    Task<CustomerOnboardingStatus> CompleteOnboardingAsync(
        Guid customerId,
        Guid adminUserId,
        CompleteOnboardingRequest request,
        CancellationToken cancellationToken = default);
}

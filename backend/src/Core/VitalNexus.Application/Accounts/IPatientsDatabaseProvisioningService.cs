using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IPatientsDatabaseProvisioningService
{
    Task<CustomerPatientsDatabase> ProvisionForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

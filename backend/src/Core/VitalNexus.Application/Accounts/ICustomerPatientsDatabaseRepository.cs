using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface ICustomerPatientsDatabaseRepository
{
    Task<CustomerPatientsDatabase?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerPatientsDatabase> CreateAsync(
        CustomerPatientsDatabase routing,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        CustomerPatientsDatabase routing,
        CancellationToken cancellationToken = default);
}

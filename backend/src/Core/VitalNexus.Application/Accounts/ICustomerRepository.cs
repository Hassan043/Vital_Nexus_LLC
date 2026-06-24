using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface ICustomerRepository
{
    Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Options;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class SimulatedPatientsDatabaseProvisioningService(
    ICustomerPatientsDatabaseRepository repository,
    IOptions<CustomerPatientsDatabaseOptions> options) : IPatientsDatabaseProvisioningService
{
    public async Task<CustomerPatientsDatabase> ProvisionForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var databaseName = $"Patients-{customerId:N}"[..Math.Min(128, $"Patients-{customerId:N}".Length)];
        var routing = new CustomerPatientsDatabase
        {
            CustomerId = customerId,
            DatabaseName = databaseName,
            ServerName = options.Value.DefaultServerName,
            IsActive = true,
            ProvisionedAt = DateTime.UtcNow,
        };

        return await repository.CreateAsync(routing, cancellationToken);
    }
}

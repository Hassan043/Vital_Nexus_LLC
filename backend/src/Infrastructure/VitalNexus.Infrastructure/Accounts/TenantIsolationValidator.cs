using VitalNexus.Application.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class TenantIsolationValidator : ITenantIsolationValidator
{
    public void EnsureUserBelongsToCustomer(Guid userCustomerId, Guid requestedCustomerId)
    {
        if (userCustomerId != requestedCustomerId)
        {
            throw new UnauthorizedAccessException("Cross-customer access is not permitted.");
        }
    }

    public void EnsureClinicBelongsToCustomer(Guid clinicCustomerId, Guid userCustomerId)
    {
        if (clinicCustomerId != userCustomerId)
        {
            throw new UnauthorizedAccessException("Clinic does not belong to the current customer.");
        }
    }
}

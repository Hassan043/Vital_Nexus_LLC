namespace VitalNexus.Application.Accounts;

public interface ITenantIsolationValidator
{
    void EnsureUserBelongsToCustomer(Guid userCustomerId, Guid requestedCustomerId);

    void EnsureClinicBelongsToCustomer(Guid clinicCustomerId, Guid userCustomerId);
}

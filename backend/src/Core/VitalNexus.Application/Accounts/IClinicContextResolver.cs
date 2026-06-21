using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IClinicContextResolver
{
    Task<ClinicContext?> ResolveAsync(
        AccountsUser user,
        Guid? requestedClinicId = null,
        CancellationToken cancellationToken = default);
}

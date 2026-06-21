using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface ICurrentAccountsUserAccessor
{
    Task<AccountsUser?> GetCurrentAsync(CancellationToken cancellationToken = default);
}

using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IExternalIdentityAccountsUserMapper
{
    Task<AccountsUser> MapAsync(
        TrustedExternalIdentity identity,
        CancellationToken cancellationToken = default);
}

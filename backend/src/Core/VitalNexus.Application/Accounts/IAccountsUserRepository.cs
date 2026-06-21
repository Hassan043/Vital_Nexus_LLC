using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IAccountsUserRepository
{
    Task<AccountsUser?> GetByEntraObjectIdAsync(Guid entraObjectId, CancellationToken cancellationToken = default);

    Task<AccountsUser> CreateAsync(AccountsUser user, CancellationToken cancellationToken = default);

    Task<AccountsUser> UpdateAsync(AccountsUser user, CancellationToken cancellationToken = default);
}

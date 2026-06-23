using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IAccountsUserRepository
{
    Task<AccountsUser?> GetByEntraObjectIdAsync(Guid entraObjectId, CancellationToken cancellationToken = default);

    Task<AccountsUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<AccountsUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountsUser>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<AccountsUser> CreateAsync(AccountsUser user, CancellationToken cancellationToken = default);

    Task<AccountsUser> UpdateAsync(AccountsUser user, CancellationToken cancellationToken = default);
}

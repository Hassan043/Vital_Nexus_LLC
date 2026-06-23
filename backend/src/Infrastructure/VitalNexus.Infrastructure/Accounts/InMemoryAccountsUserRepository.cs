using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryAccountsUserRepository : IAccountsUserRepository
{
    private readonly ConcurrentDictionary<Guid, AccountsUser> _usersByEntraObjectId = new();

    public Task<AccountsUser?> GetByEntraObjectIdAsync(
        Guid entraObjectId,
        CancellationToken cancellationToken = default)
    {
        _usersByEntraObjectId.TryGetValue(entraObjectId, out var user);
        return Task.FromResult(user);
    }

    public Task<AccountsUser> CreateAsync(AccountsUser user, CancellationToken cancellationToken = default)
    {
        if (!_usersByEntraObjectId.TryAdd(user.EntraObjectId, user))
        {
            throw new InvalidOperationException("An Accounts user already exists for the Entra object id.");
        }

        return Task.FromResult(user);
    }

    public Task<AccountsUser> UpdateAsync(AccountsUser user, CancellationToken cancellationToken = default)
    {
        _usersByEntraObjectId[user.EntraObjectId] = user;
        return Task.FromResult(user);
    }
}

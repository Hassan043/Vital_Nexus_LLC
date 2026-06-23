using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryAccountsUserRepository : IAccountsUserRepository
{
    private readonly ConcurrentDictionary<Guid, AccountsUser> _usersByEntraObjectId = new();
    private readonly ConcurrentDictionary<Guid, AccountsUser> _usersById = new();

    public Task<AccountsUser?> GetByEntraObjectIdAsync(
        Guid entraObjectId,
        CancellationToken cancellationToken = default)
    {
        _usersByEntraObjectId.TryGetValue(entraObjectId, out var user);
        return Task.FromResult(user);
    }

    public Task<AccountsUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _usersById.TryGetValue(userId, out var user);
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<AccountsUser>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var users = _usersById.Values
            .Where(user => user.CustomerId == customerId)
            .ToList();

        return Task.FromResult<IReadOnlyList<AccountsUser>>(users);
    }

    public Task<AccountsUser> CreateAsync(AccountsUser user, CancellationToken cancellationToken = default)
    {
        if (!_usersByEntraObjectId.TryAdd(user.EntraObjectId, user))
        {
            throw new InvalidOperationException("An Accounts user already exists for the Entra object id.");
        }

        _usersById[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<AccountsUser> UpdateAsync(AccountsUser user, CancellationToken cancellationToken = default)
    {
        _usersByEntraObjectId[user.EntraObjectId] = user;
        _usersById[user.Id] = user;
        return Task.FromResult(user);
    }
}

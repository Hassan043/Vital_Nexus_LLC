using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryUserRoleRepository : IUserRoleRepository
{
    private static readonly HashSet<string> KnownRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationRoles.Admin,
        ApplicationRoles.User,
    };

    private readonly ConcurrentDictionary<(Guid UserId, string RoleName), byte> _assignments = new();
    private readonly ConcurrentDictionary<Guid, Guid> _adminByCustomer = new();

    public Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var roles = _assignments.Keys
            .Where(key => key.UserId == userId)
            .Select(key => key.RoleName)
            .OrderBy(roleName => roleName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(roles);
    }

    public Task AssignRoleAsync(
        Guid userId,
        Guid customerId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        if (!KnownRoles.Contains(roleName))
        {
            throw new InvalidOperationException($"Unknown application role '{roleName}'.");
        }

        if (string.Equals(roleName, ApplicationRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (_adminByCustomer.TryGetValue(customerId, out var existingAdminId)
                && existingAdminId != userId)
            {
                throw new InvalidOperationException(
                    "This customer already has an active Admin. Only one Admin is allowed per customer.");
            }

            _adminByCustomer[customerId] = userId;
        }

        _assignments.TryAdd((userId, roleName), 0);
        return Task.CompletedTask;
    }

    public Task RemoveAllRolesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        foreach (var key in _assignments.Keys.Where(entry => entry.UserId == userId).ToList())
        {
            _assignments.TryRemove(key, out _);
        }

        foreach (var customerId in _adminByCustomer
                     .Where(entry => entry.Value == userId)
                     .Select(entry => entry.Key)
                     .ToList())
        {
            _adminByCustomer.TryRemove(customerId, out _);
        }

        return Task.CompletedTask;
    }
}

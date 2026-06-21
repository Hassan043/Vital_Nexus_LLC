using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryUserRoleRepository : IUserRoleRepository
{
    private static readonly HashSet<string> KnownRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationRoles.Clinician,
        ApplicationRoles.ClinicAdmin,
    };

    private readonly ConcurrentDictionary<(Guid UserId, string RoleName), byte> _assignments = new();

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
        string roleName,
        CancellationToken cancellationToken = default)
    {
        if (!KnownRoles.Contains(roleName))
        {
            throw new InvalidOperationException($"Unknown application role '{roleName}'.");
        }

        _assignments.TryAdd((userId, roleName), 0);
        return Task.CompletedTask;
    }
}

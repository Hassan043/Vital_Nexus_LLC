using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryClinicMembershipRepository : IClinicMembershipRepository
{
    private readonly ConcurrentDictionary<(Guid UserId, Guid ClinicId), ClinicMembership> _memberships = new();

    public Task<IReadOnlyList<ClinicMembership>> GetMembershipsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var memberships = _memberships
            .Where(entry => entry.Key.UserId == userId)
            .Select(entry => entry.Value)
            .OrderBy(membership => membership.ClinicName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<ClinicMembership>>(memberships);
    }

    public Task AddMembershipAsync(
        Guid userId,
        ClinicMembership membership,
        CancellationToken cancellationToken = default)
    {
        _memberships[(userId, membership.ClinicId)] = membership;
        return Task.CompletedTask;
    }
}

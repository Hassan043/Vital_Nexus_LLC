using System.Collections.Concurrent;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class InMemoryUserInvitationRepository : IUserInvitationRepository
{
    private readonly ConcurrentDictionary<Guid, UserInvitation> _invitations = new();

    public Task<UserInvitation> CreateAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
    {
        if (!_invitations.TryAdd(invitation.Id, invitation))
        {
            throw new InvalidOperationException("An invitation with the same id already exists.");
        }

        return Task.FromResult(invitation);
    }

    public Task<UserInvitation?> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var invitation = _invitations.Values.FirstOrDefault(
            entry => entry.AcceptedAt is null
                && string.Equals(entry.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(invitation);
    }

    public Task<UserInvitation> MarkAcceptedAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        if (!_invitations.TryGetValue(invitationId, out var invitation))
        {
            throw new InvalidOperationException("Invitation was not found.");
        }

        var accepted = new UserInvitation
        {
            Id = invitation.Id,
            CustomerId = invitation.CustomerId,
            Email = invitation.Email,
            RoleName = invitation.RoleName,
            InvitedByUserId = invitation.InvitedByUserId,
            ClinicIds = invitation.ClinicIds,
            CreatedAt = invitation.CreatedAt,
            AcceptedAt = DateTime.UtcNow,
        };

        _invitations[invitationId] = accepted;
        return Task.FromResult(accepted);
    }

    public Task<IReadOnlyList<UserInvitation>> GetPendingByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var invitations = _invitations.Values
            .Where(invitation => invitation.CustomerId == customerId && invitation.AcceptedAt is null)
            .OrderBy(invitation => invitation.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<UserInvitation>>(invitations);
    }
}

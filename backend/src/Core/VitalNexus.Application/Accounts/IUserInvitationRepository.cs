using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IUserInvitationRepository
{
    Task<UserInvitation> CreateAsync(UserInvitation invitation, CancellationToken cancellationToken = default);

    Task<UserInvitation?> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<UserInvitation> MarkAcceptedAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserInvitation>> GetPendingByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

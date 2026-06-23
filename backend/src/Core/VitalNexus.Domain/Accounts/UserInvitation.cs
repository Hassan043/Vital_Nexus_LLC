namespace VitalNexus.Domain.Accounts;

public sealed class UserInvitation
{
    public Guid Id { get; init; }

    public Guid CustomerId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string RoleName { get; init; } = ApplicationRoles.User;

    public Guid InvitedByUserId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? AcceptedAt { get; init; }
}

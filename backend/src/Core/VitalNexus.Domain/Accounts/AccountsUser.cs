namespace VitalNexus.Domain.Accounts;

public sealed class AccountsUser
{
    public Guid Id { get; init; }

    public Guid? EntraObjectId { get; init; }

    public Guid CustomerId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public string AccountStatus { get; init; } = AccountStatuses.Active;

    public DateTime CreatedAt { get; init; }

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<ClinicMembership> ClinicMemberships { get; init; } = [];
}

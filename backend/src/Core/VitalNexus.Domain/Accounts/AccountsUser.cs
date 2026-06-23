namespace VitalNexus.Domain.Accounts;

public sealed class AccountsUser
{
    public Guid Id { get; init; }

    public Guid EntraObjectId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public DateTime CreatedAt { get; init; }
}

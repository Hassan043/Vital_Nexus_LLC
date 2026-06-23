namespace VitalNexus.Application.Identity;

public sealed record TrustedExternalIdentity
{
    public required string ObjectId { get; init; }

    public string? Subject { get; init; }

    public string? TenantId { get; init; }

    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyList<string> Scopes { get; init; } = [];
}

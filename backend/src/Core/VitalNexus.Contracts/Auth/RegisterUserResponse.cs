namespace VitalNexus.Contracts.Auth;

public sealed class RegisterUserResponse
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}

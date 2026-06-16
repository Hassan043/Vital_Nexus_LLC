namespace VitalNexus.Application.Auth.RegisterUser;

public enum RegisterUserErrorCode
{
    None = 0,
    InvalidEmail,
    PasswordTooShort,
    PasswordMismatch,
    DuplicateEmail,
}

public sealed record RegisterUserResult
{
    public bool Succeeded { get; init; }

    public RegisterUserErrorCode ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public string? AccessToken { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    public static RegisterUserResult Success(
        Guid userId,
        string email,
        string? displayName,
        string accessToken,
        DateTime expiresAtUtc) =>
        new()
        {
            Succeeded = true,
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            AccessToken = accessToken,
            ExpiresAtUtc = expiresAtUtc,
        };

    public static RegisterUserResult Failure(RegisterUserErrorCode errorCode, string errorMessage) =>
        new()
        {
            Succeeded = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
        };
}

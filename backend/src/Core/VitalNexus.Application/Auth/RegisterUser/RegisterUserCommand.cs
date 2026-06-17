namespace VitalNexus.Application.Auth.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string? DisplayName);

using System.Net.Mail;
using VitalNexus.Application.Abstractions;
using VitalNexus.Domain.Entities;

namespace VitalNexus.Application.Auth.RegisterUser;

public sealed class RegisterUserService : IRegisterUserService
{
    public const int MinimumPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public RegisterUserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterUserResult> RegisterAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationError = Validate(command);
        if (validationError is not null)
        {
            return validationError;
        }

        var normalizedEmail = NormalizeEmail(command.Email);

        if (await _userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            return RegisterUserResult.Failure(
                RegisterUserErrorCode.DuplicateEmail,
                "An account with this email already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(command.Password);
        var user = User.Create(
            command.Email,
            normalizedEmail,
            command.DisplayName,
            passwordHash,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _userRepository.AddAsync(user, cancellationToken);

        var token = _jwtTokenGenerator.GenerateAccessToken(user.Id, user.Email);

        return RegisterUserResult.Success(
            user.Id,
            user.Email,
            user.DisplayName,
            token.AccessToken,
            token.ExpiresAtUtc);
    }

    internal static string NormalizeEmail(string email) =>
        email.Trim().ToUpperInvariant();

    private static RegisterUserResult? Validate(RegisterUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email) || !IsValidEmail(command.Email))
        {
            return RegisterUserResult.Failure(
                RegisterUserErrorCode.InvalidEmail,
                "A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Password)
            || command.Password.Length < MinimumPasswordLength)
        {
            return RegisterUserResult.Failure(
                RegisterUserErrorCode.PasswordTooShort,
                $"Password must be at least {MinimumPasswordLength} characters.");
        }

        if (!string.Equals(command.Password, command.ConfirmPassword, StringComparison.Ordinal))
        {
            return RegisterUserResult.Failure(
                RegisterUserErrorCode.PasswordMismatch,
                "Password and confirmation password do not match.");
        }

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email.Trim());
            return string.Equals(
                address.Address,
                email.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

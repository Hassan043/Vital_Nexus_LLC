using VitalNexus.Application.Abstractions;
using VitalNexus.Application.Auth.RegisterUser;
using VitalNexus.Domain.Entities;

namespace VitalNexus.UnitTests.Auth;

public sealed class RegisterUserServiceTests
{
    private static readonly DateTime FixedUtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsSuccessWithAccessToken()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var jwtGenerator = new FakeJwtTokenGenerator();
        var service = CreateService(repository, passwordHasher, jwtGenerator);

        var result = await service.RegisterAsync(
            new RegisterUserCommand(
                "provider@example.com",
                "Password1!",
                "Password1!",
                "Dr. Example"),
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.UserId);
        Assert.Equal("provider@example.com", result.Email);
        Assert.Equal("Dr. Example", result.DisplayName);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal(FixedUtcNow.AddHours(24), result.ExpiresAtUtc);
        Assert.Single(repository.Users);
        Assert.Equal("hashed:Password1!", repository.Users[0].PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsDuplicateEmailFailure()
    {
        var repository = new FakeUserRepository
        {
            ExistingNormalizedEmails = { "PROVIDER@EXAMPLE.COM" },
        };
        var service = CreateService(repository, new FakePasswordHasher(), new FakeJwtTokenGenerator());

        var result = await service.RegisterAsync(
            new RegisterUserCommand(
                "provider@example.com",
                "Password1!",
                "Password1!",
                null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterUserErrorCode.DuplicateEmail, result.ErrorCode);
        Assert.Empty(repository.Users);
    }

    [Theory]
    [InlineData("", "Password1!", "Password1!")]
    [InlineData("not-an-email", "Password1!", "Password1!")]
    public async Task RegisterAsync_WithInvalidEmail_ReturnsInvalidEmailFailure(
        string email,
        string password,
        string confirmPassword)
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, new FakePasswordHasher(), new FakeJwtTokenGenerator());

        var result = await service.RegisterAsync(
            new RegisterUserCommand(email, password, confirmPassword, null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterUserErrorCode.InvalidEmail, result.ErrorCode);
        Assert.Empty(repository.Users);
    }

    [Fact]
    public async Task RegisterAsync_WithShortPassword_ReturnsPasswordTooShortFailure()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, new FakePasswordHasher(), new FakeJwtTokenGenerator());

        var result = await service.RegisterAsync(
            new RegisterUserCommand("provider@example.com", "short", "short", null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterUserErrorCode.PasswordTooShort, result.ErrorCode);
        Assert.Empty(repository.Users);
    }

    [Fact]
    public async Task RegisterAsync_WithMismatchedPasswords_ReturnsPasswordMismatchFailure()
    {
        var repository = new FakeUserRepository();
        var service = CreateService(repository, new FakePasswordHasher(), new FakeJwtTokenGenerator());

        var result = await service.RegisterAsync(
            new RegisterUserCommand("provider@example.com", "Password1!", "Password2!", null),
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(RegisterUserErrorCode.PasswordMismatch, result.ErrorCode);
        Assert.Empty(repository.Users);
    }

    [Fact]
    public void NormalizeEmail_UsesUpperInvariantTrimmedValue()
    {
        Assert.Equal("USER@EXAMPLE.COM", RegisterUserService.NormalizeEmail("  user@example.com "));
    }

    private static RegisterUserService CreateService(
        FakeUserRepository repository,
        FakePasswordHasher passwordHasher,
        FakeJwtTokenGenerator jwtGenerator) =>
        new(
            repository,
            passwordHasher,
            jwtGenerator,
            new FakeTimeProvider(FixedUtcNow));

    private sealed class FakeUserRepository : IUserRepository
    {
        public HashSet<string> ExistingNormalizedEmails { get; } = new(StringComparer.Ordinal);

        public List<User> Users { get; } = [];

        public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingNormalizedEmails.Contains(normalizedEmail));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            ExistingNormalizedEmails.Add(user.NormalizedEmail);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string password, string passwordHash) =>
            passwordHash == $"hashed:{password}";
    }

    private sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public JwtToken GenerateAccessToken(Guid userId, string email) =>
            new("test-access-token", FixedUtcNow.AddHours(24));
    }

    private sealed class FakeTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}

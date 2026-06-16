using VitalNexus.Application.Abstractions;

namespace VitalNexus.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public const int WorkFactor = 10;

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool VerifyPassword(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}

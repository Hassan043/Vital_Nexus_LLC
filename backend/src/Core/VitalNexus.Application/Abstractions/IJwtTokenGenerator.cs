namespace VitalNexus.Application.Abstractions;

public interface IJwtTokenGenerator
{
    JwtToken GenerateAccessToken(Guid userId, string email);
}

public sealed record JwtToken(string AccessToken, DateTime ExpiresAtUtc);

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VitalNexus.Application.Abstractions;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.Infrastructure.Security;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public JwtToken GenerateAccessToken(Guid userId, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(_options.Audience);

        var expiresAtUtc = _timeProvider.GetUtcNow().UtcDateTime
            .AddHours(_options.TokenLifetimeHours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _timeProvider.GetUtcNow().UtcDateTime,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtToken(accessToken, expiresAtUtc);
    }
}

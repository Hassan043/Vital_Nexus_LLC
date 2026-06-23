using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace VitalNexus.IntegrationTests.Support;

public sealed class EntraExternalIdTestTokenBuilder
{
    public const string DefaultAuthority =
        "https://vitalnexusexternal.ciamlogin.com/00000000-0000-4000-8000-000000000001/v2.0";

    public const string DefaultAudience = "11111111-1111-1111-1111-111111111111";

    public const string DefaultApplicationIdUri =
        "https://vitalnexusexternal.onmicrosoft.com/vitalnexus-api";

    public static readonly SymmetricSecurityKey DefaultSigningKey =
        new(Encoding.UTF8.GetBytes("integration-test-signing-key-32-bytes!!"));

    private static readonly SymmetricSecurityKey AlternateSigningKey =
        new(Encoding.UTF8.GetBytes("alternate-integration-test-sign-key!"));

    public string Issuer { get; init; } = DefaultAuthority;

    public string Audience { get; init; } = DefaultAudience;

    public SymmetricSecurityKey SigningKey { get; init; } = DefaultSigningKey;

    public DateTime ExpiresUtc { get; init; } = DateTime.UtcNow.AddMinutes(5);

    public string Scopes { get; init; } = "openid profile access_as_user";

    public string ObjectId { get; init; } = "00000000-0000-4000-8000-000000000099";

    public string Email { get; init; } = "clinician@example.com";

    public static EntraExternalIdTestTokenBuilder Valid() => new();

    public EntraExternalIdTestTokenBuilder WithObjectId(string objectId) =>
        new()
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            ExpiresUtc = ExpiresUtc,
            Scopes = Scopes,
            ObjectId = objectId,
            Email = Email,
        };

    public EntraExternalIdTestTokenBuilder WithEmail(string email) =>
        new()
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            ExpiresUtc = ExpiresUtc,
            Scopes = Scopes,
            ObjectId = ObjectId,
            Email = email,
        };

    public static EntraExternalIdTestTokenBuilder Expired() =>
        new() { ExpiresUtc = DateTime.UtcNow.AddMinutes(-5) };

    public static EntraExternalIdTestTokenBuilder WithWrongIssuer() =>
        new() { Issuer = "https://invalid.example.com/tenant/v2.0" };

    public static EntraExternalIdTestTokenBuilder WithWrongAudience() =>
        new() { Audience = "22222222-2222-2222-2222-222222222222" };

    public static EntraExternalIdTestTokenBuilder WithWrongSigningKey() =>
        new() { SigningKey = AlternateSigningKey };

    public static EntraExternalIdTestTokenBuilder WithApplicationIdUriAudience() =>
        new() { Audience = DefaultApplicationIdUri };

    public static EntraExternalIdTestTokenBuilder WithoutRequiredScope() =>
        new() { Scopes = "openid profile" };

    public string Build()
    {
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim("sub", ObjectId),
                new Claim("oid", ObjectId),
                new Claim("name", "Test Clinician"),
                new Claim("preferred_username", Email),
                new Claim("scp", Scopes),
            ],
            expires: ExpiresUtc,
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

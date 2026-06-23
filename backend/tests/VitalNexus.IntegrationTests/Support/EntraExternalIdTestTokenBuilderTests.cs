using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace VitalNexus.IntegrationTests.Support;

public sealed class EntraExternalIdTestTokenBuilderTests
{
    [Fact]
    public void Build_ProducesParsableJwtForValidBuilder()
    {
        var token = EntraExternalIdTestTokenBuilder.Valid().Build();
        var handler = new JwtSecurityTokenHandler();

        Assert.True(handler.CanReadToken(token));

        var parsed = handler.ReadJwtToken(token);
        Assert.Equal(EntraExternalIdTestTokenBuilder.DefaultAuthority, parsed.Issuer);
        Assert.Contains(EntraExternalIdTestTokenBuilder.DefaultAudience, parsed.Audiences);
    }

    [Fact]
    public void Expired_BuildsTokenWithPastExpiry()
    {
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(
            EntraExternalIdTestTokenBuilder.Expired().Build());

        Assert.True(parsed.ValidTo < DateTime.UtcNow);
    }
}

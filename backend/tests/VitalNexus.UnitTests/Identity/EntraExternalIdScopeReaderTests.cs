using System.Security.Claims;
using VitalNexus.Infrastructure.Identity;

namespace VitalNexus.UnitTests.Identity;

public sealed class EntraExternalIdScopeReaderTests
{
    [Fact]
    public void HasRequiredScope_ReturnsTrueWhenScopeClaimContainsRequiredScope()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scp", "openid profile access_as_user"),
        ], authenticationType: "Bearer"));

        Assert.True(EntraExternalIdScopeReader.HasRequiredScope(user, "access_as_user"));
    }

    [Fact]
    public void HasRequiredScope_ReturnsFalseWhenScopeClaimIsMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-id"),
        ], authenticationType: "Bearer"));

        Assert.False(EntraExternalIdScopeReader.HasRequiredScope(user, "access_as_user"));
    }

    [Fact]
    public void ReadScopes_ParsesSpaceDelimitedScopeClaim()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scp", "openid profile access_as_user"),
        ], authenticationType: "Bearer"));

        var scopes = EntraExternalIdScopeReader.ReadScopes(user);

        Assert.Equal(["openid", "profile", "access_as_user"], scopes);
    }
}

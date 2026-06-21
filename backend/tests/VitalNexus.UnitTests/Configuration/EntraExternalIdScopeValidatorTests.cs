using System.Security.Claims;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.UnitTests.Configuration;

public sealed class EntraExternalIdScopeValidatorTests
{
    [Fact]
    public void HasRequiredScope_ReturnsTrueWhenScopeClaimContainsRequiredScope()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scp", "openid profile access_as_user"),
        ]));

        Assert.True(EntraExternalIdScopeValidator.HasRequiredScope(user, "access_as_user"));
    }

    [Fact]
    public void HasRequiredScope_ReturnsFalseWhenScopeClaimIsMissing()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-id"),
        ]));

        Assert.False(EntraExternalIdScopeValidator.HasRequiredScope(user, "access_as_user"));
    }

    [Fact]
    public void HasRequiredScope_ReturnsFalseWhenRequiredScopeDoesNotMatch()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scp", "openid profile"),
        ]));

        Assert.False(EntraExternalIdScopeValidator.HasRequiredScope(user, "access_as_user"));
    }
}

using System.Security.Claims;
using VitalNexus.Infrastructure.Identity;

namespace VitalNexus.UnitTests.Identity;

public sealed class ExternalIdentityClaimsReaderTests
{
    [Fact]
    public void TryRead_ReturnsNullForUnauthenticatedPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Null(ExternalIdentityClaimsReader.TryRead(principal));
    }

    [Fact]
    public void TryRead_ReturnsNullWhenObjectIdentifierClaimsAreMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("name", "Test Clinician"),
        ], authenticationType: "Bearer"));

        Assert.Null(ExternalIdentityClaimsReader.TryRead(principal));
    }

    [Fact]
    public void TryRead_ExtractsCiamClaimsFromValidatedTokenPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("oid", "00000000-0000-4000-8000-000000000099"),
            new Claim("sub", "00000000-0000-4000-8000-000000000099"),
            new Claim("tid", "00000000-0000-4000-8000-000000000001"),
            new Claim("name", "Test Clinician"),
            new Claim("preferred_username", "clinician@example.com"),
            new Claim("scp", "openid profile access_as_user"),
        ], authenticationType: "Bearer"));

        var identity = ExternalIdentityClaimsReader.TryRead(principal);

        Assert.NotNull(identity);
        Assert.Equal("00000000-0000-4000-8000-000000000099", identity!.ObjectId);
        Assert.Equal("00000000-0000-4000-8000-000000000099", identity.Subject);
        Assert.Equal("00000000-0000-4000-8000-000000000001", identity.TenantId);
        Assert.Equal("Test Clinician", identity.DisplayName);
        Assert.Equal("clinician@example.com", identity.Email);
        Assert.Equal(["openid", "profile", "access_as_user"], identity.Scopes);
    }

    [Fact]
    public void TryRead_FallsBackToSubjectWhenObjectIdClaimIsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "subject-user-id"),
            new Claim("preferred_username", "clinician@example.com"),
        ], authenticationType: "Bearer"));

        var identity = ExternalIdentityClaimsReader.TryRead(principal);

        Assert.NotNull(identity);
        Assert.Equal("subject-user-id", identity!.ObjectId);
        Assert.Equal("subject-user-id", identity.Subject);
    }

    [Fact]
    public void TryRead_ParsesEmailsClaimJsonArray()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("oid", "00000000-0000-4000-8000-000000000099"),
            new Claim("emails", "[\"first@example.com\",\"second@example.com\"]"),
        ], authenticationType: "Bearer"));

        var identity = ExternalIdentityClaimsReader.TryRead(principal);

        Assert.NotNull(identity);
        Assert.Equal("first@example.com", identity!.Email);
    }
}

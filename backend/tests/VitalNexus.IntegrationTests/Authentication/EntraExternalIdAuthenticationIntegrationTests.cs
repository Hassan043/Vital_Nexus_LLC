using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.IntegrationTests.Authentication;

public sealed class EntraExternalIdAuthenticationIntegrationTests : IClassFixture<EntraExternalIdWebApplicationFactory>
{
    private readonly EntraExternalIdWebApplicationFactory _factory;

    public EntraExternalIdAuthenticationIntegrationTests(EntraExternalIdWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_AllowsAnonymousAccess()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutBearerToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidAccessToken_ReturnsProfileClaims()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateAccessToken(includeRequiredScope: true));

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("access_as_user", payload, StringComparison.Ordinal);
        Assert.Contains("clinician@example.com", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Me_WithMissingScope_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateAccessToken(includeRequiredScope: false));

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed class EntraExternalIdWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestAuthority = "https://vitalnexusexternal.ciamlogin.com/00000000-0000-4000-8000-000000000001/v2.0";
    private const string TestAudience = "11111111-1111-1111-1111-111111111111";
    private static readonly SymmetricSecurityKey SigningKey =
        new(Encoding.UTF8.GetBytes("integration-test-signing-key-32-bytes!!"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("B2C_TENANT_ID", "00000000-0000-4000-8000-000000000001");
        Environment.SetEnvironmentVariable("B2C_API_CLIENT_ID", TestAudience);
        Environment.SetEnvironmentVariable("B2C_TENANT_DOMAIN_PREFIX", "vitalnexusexternal");
        Environment.SetEnvironmentVariable("B2C_TENANT_KIND", "ciam");
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000");

        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null!;
                options.MetadataAddress = null;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestAuthority,
                    ValidateAudience = true,
                    ValidAudiences = [TestAudience],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = SigningKey,
                    NameClaimType = "name",
                };
            });
        });
    }

    public string CreateAccessToken(bool includeRequiredScope)
    {
        var scopes = includeRequiredScope ? "openid profile access_as_user" : "openid profile";
        var token = new JwtSecurityToken(
            issuer: TestAuthority,
            audience: TestAudience,
            claims:
            [
                new Claim("sub", "00000000-0000-4000-8000-000000000099"),
                new Claim("oid", "00000000-0000-4000-8000-000000000099"),
                new Claim("name", "Test Clinician"),
                new Claim("preferred_username", "clinician@example.com"),
                new Claim("scp", scopes),
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace VitalNexus.IntegrationTests.Support;

public sealed class EntraExternalIdWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("B2C_TENANT_ID", "00000000-0000-4000-8000-000000000001");
        Environment.SetEnvironmentVariable("B2C_API_CLIENT_ID", EntraExternalIdTestTokenBuilder.DefaultAudience);
        Environment.SetEnvironmentVariable("B2C_TENANT_DOMAIN_PREFIX", "vitalnexusexternal");
        Environment.SetEnvironmentVariable("B2C_TENANT_KIND", "ciam");
        Environment.SetEnvironmentVariable(
            "B2C_API_APPLICATION_ID_URI",
            EntraExternalIdTestTokenBuilder.DefaultApplicationIdUri);
        Environment.SetEnvironmentVariable(
            "APPLICATIONINSIGHTS_CONNECTION_STRING",
            "InstrumentationKey=00000000-0000-0000-0000-000000000000");

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PatientHealth"] =
                    "Server=localhost;Database=PatientHealth;Trusted_Connection=True;Encrypt=False",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null!;
                options.MetadataAddress = null!;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = EntraExternalIdTestTokenBuilder.DefaultAuthority,
                    ValidateAudience = true,
                    ValidAudiences =
                    [
                        EntraExternalIdTestTokenBuilder.DefaultAudience,
                        EntraExternalIdTestTokenBuilder.DefaultApplicationIdUri,
                    ],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = EntraExternalIdTestTokenBuilder.DefaultSigningKey,
                    NameClaimType = "name",
                };
            });
        });
    }

    public string CreateAccessToken(bool includeRequiredScope) =>
        includeRequiredScope
            ? EntraExternalIdTestTokenBuilder.Valid().Build()
            : EntraExternalIdTestTokenBuilder.WithoutRequiredScope().Build();
}

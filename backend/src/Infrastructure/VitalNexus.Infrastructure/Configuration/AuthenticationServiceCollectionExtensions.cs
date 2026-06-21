using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VitalNexus.Infrastructure.Identity;

namespace VitalNexus.Infrastructure.Configuration;

public static class EntraExternalIdAuthenticationExtensions
{
    public const string ApiAccessPolicyName = "ApiAccess";

    public static IServiceCollection AddEntraExternalIdAuthentication(
        this IServiceCollection services,
        EntraExternalIdOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Entra External ID authentication requires TenantId, TenantDomainPrefix, and ApiClientId.");
        }

        services.AddSingleton(Options.Create(options));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.Authority = options.Authority;
                jwtOptions.MapInboundClaims = false;
                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudiences = options.GetValidAudiences(),
                    NameClaimType = "name",
                    RoleClaimType = ClaimTypes.Role,
                };

                jwtOptions.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("EntraExternalId.JwtBearer")
                            .LogWarning(
                                context.Exception,
                                "Entra External ID access token validation failed.");

                        return Task.CompletedTask;
                    },
                };
            });

        var apiAccessPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                EntraExternalIdScopeReader.HasRequiredScope(context.User, options.RequiredScope))
            .Build();

        services.AddAuthorizationBuilder()
            .AddPolicy(ApiAccessPolicyName, policy =>
                ConfigureApiAccessPolicy(policy, options))
            .SetFallbackPolicy(apiAccessPolicy);

        return services;
    }

    private static void ConfigureApiAccessPolicy(
        AuthorizationPolicyBuilder policy,
        EntraExternalIdOptions options)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            EntraExternalIdScopeReader.HasRequiredScope(context.User, options.RequiredScope));
    }

    public static IServiceCollection AddVitalNexusCors(
        this IServiceCollection services,
        EntraExternalIdOptions options)
    {
        var origins = options.AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (origins.Length == 0)
        {
            return services;
        }

        services.AddCors(corsOptions =>
        {
            corsOptions.AddPolicy("Spa", policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}

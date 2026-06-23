using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

        services.AddAuthorizationBuilder()
            .AddPolicy(ApiAccessPolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context =>
                    EntraExternalIdScopeValidator.HasRequiredScope(context.User, options.RequiredScope));
            });

        return services;
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

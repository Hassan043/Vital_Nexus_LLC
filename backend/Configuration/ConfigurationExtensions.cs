using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NutrientInsight.Api.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddEnvironmentConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"));
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.Configure<AnthropicOptions>(configuration.GetSection("Anthropic"));
        services.Configure<AppOptions>(configuration);

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace VitalNexus.Infrastructure.Identity;

public static class ExternalIdentityServiceCollectionExtensions
{
    public static IServiceCollection AddExternalIdentityAccessor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<Application.Identity.IExternalIdentityAccessor, HttpContextExternalIdentityAccessor>();
        return services;
    }
}

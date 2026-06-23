using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public static class ClinicContextServiceCollectionExtensions
{
    public static IServiceCollection AddClinicContextResolution(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ClinicPatientsDatabaseOptions>(
            configuration.GetSection(ClinicPatientsDatabaseOptions.SectionName));
        services.AddSingleton<IClinicPatientsDatabaseRepository, InMemoryClinicPatientsDatabaseRepository>();
        services.AddSingleton<PatientsDatabaseConnectionStringFactory>();
        services.AddScoped<IClinicContextResolver, ClinicContextResolver>();
        services.AddScoped<ICurrentClinicContextAccessor, HttpContextCurrentClinicContextAccessor>();
        return services;
    }
}

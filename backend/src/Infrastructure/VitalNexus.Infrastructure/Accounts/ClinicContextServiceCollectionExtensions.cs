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
        services.Configure<CustomerPatientsDatabaseOptions>(
            configuration.GetSection(CustomerPatientsDatabaseOptions.SectionName));
        services.AddSingleton<ICustomerPatientsDatabaseRepository, InMemoryCustomerPatientsDatabaseRepository>();
        services.AddSingleton<IPatientsDatabaseProvisioningService, SimulatedPatientsDatabaseProvisioningService>();
        services.AddSingleton<PatientsDatabaseConnectionStringFactory>();
        services.AddScoped<IClinicContextResolver, ClinicContextResolver>();
        services.AddScoped<ICurrentClinicContextAccessor, HttpContextCurrentClinicContextAccessor>();
        return services;
    }
}
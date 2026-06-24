using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public static class AccountsUserServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsUserMapping(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAccountsRepositories(configuration);
        services.AddSingleton<ITenantIsolationValidator, TenantIsolationValidator>();
        services.AddScoped<ICustomerOnboardingService, CustomerOnboardingService>();
        services.AddScoped<IBillingQuoteService, BillingQuoteService>();
        services.AddScoped<IExternalIdentityAccountsUserMapper, ExternalIdentityAccountsUserMapper>();
        services.AddScoped<ICurrentAccountsUserAccessor, HttpContextCurrentAccountsUserAccessor>();
        return services;
    }
}

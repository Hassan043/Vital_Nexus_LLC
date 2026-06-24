using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public static class AccountsUserServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsUserMapping(this IServiceCollection services)
    {
        services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
        services.AddSingleton<IAccountsUserRepository, InMemoryAccountsUserRepository>();
        services.AddSingleton<IUserRoleRepository, InMemoryUserRoleRepository>();
        services.AddSingleton<IClinicMembershipRepository, InMemoryClinicMembershipRepository>();
        services.AddSingleton<IClinicRepository, InMemoryClinicRepository>();
        services.AddSingleton<IClinicProfileRepository, InMemoryClinicProfileRepository>();
        services.AddSingleton<IUserInvitationRepository, InMemoryUserInvitationRepository>();
        services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
        services.AddSingleton<IPlanTierRepository, InMemoryPlanTierRepository>();
        services.AddSingleton<IBaaAgreementRepository, InMemoryBaaAgreementRepository>();
        services.AddSingleton<ICustomerOnboardingStateRepository, InMemoryCustomerOnboardingStateRepository>();
        services.AddSingleton<IOnboardingAuditRepository, InMemoryOnboardingAuditRepository>();
        services.AddSingleton<ITenantIsolationValidator, TenantIsolationValidator>();
        services.AddScoped<ICustomerOnboardingService, CustomerOnboardingService>();
        services.AddScoped<IBillingQuoteService, BillingQuoteService>();
        services.AddScoped<IExternalIdentityAccountsUserMapper, ExternalIdentityAccountsUserMapper>();
        services.AddScoped<ICurrentAccountsUserAccessor, HttpContextCurrentAccountsUserAccessor>();
        return services;
    }
}
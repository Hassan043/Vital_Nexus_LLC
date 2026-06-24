using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Accounts;
using VitalNexus.Infrastructure.Accounts.Sql;

namespace VitalNexus.Infrastructure.Accounts;

public static class AccountsRepositoriesServiceCollectionExtensions
{
    public static bool UseSqlAccountsDataStore(this IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Accounts"));

    public static IServiceCollection AddAccountsRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (configuration.UseSqlAccountsDataStore())
        {
            services.AddSingleton<IAccountsDbConnectionFactory>(
                _ => new AccountsDbConnectionFactory(configuration.GetConnectionString("Accounts")!));
            services.AddSingleton<ICustomerRepository, SqlCustomerRepository>();
            services.AddSingleton<IAccountsUserRepository, SqlAccountsUserRepository>();
            services.AddSingleton<IUserRoleRepository, SqlUserRoleRepository>();
            services.AddSingleton<IClinicMembershipRepository, SqlClinicMembershipRepository>();
            services.AddSingleton<IClinicRepository, SqlClinicRepository>();
            services.AddSingleton<IClinicProfileRepository, SqlClinicProfileRepository>();
            services.AddSingleton<IUserInvitationRepository, SqlUserInvitationRepository>();
            services.AddSingleton<ISubscriptionRepository, SqlSubscriptionRepository>();
            services.AddSingleton<IPlanTierRepository, SqlPlanTierRepository>();
            services.AddSingleton<IBaaAgreementRepository, SqlBaaAgreementRepository>();
            services.AddSingleton<ICustomerOnboardingStateRepository, SqlCustomerOnboardingStateRepository>();
            services.AddSingleton<IOnboardingAuditRepository, SqlOnboardingAuditRepository>();
            services.AddSingleton<ICustomerPatientsDatabaseRepository, SqlCustomerPatientsDatabaseRepository>();
        }
        else
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
            services.AddSingleton<ICustomerPatientsDatabaseRepository, InMemoryCustomerPatientsDatabaseRepository>();
        }

        return services;
    }
}

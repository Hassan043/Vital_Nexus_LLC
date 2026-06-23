using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public static class AccountsUserServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsUserMapping(this IServiceCollection services)
    {
        services.AddSingleton<IAccountsUserRepository, InMemoryAccountsUserRepository>();
        services.AddSingleton<IUserRoleRepository, InMemoryUserRoleRepository>();
        services.AddSingleton<IClinicMembershipRepository, InMemoryClinicMembershipRepository>();
        services.AddScoped<IExternalIdentityAccountsUserMapper, ExternalIdentityAccountsUserMapper>();
        services.AddScoped<ICurrentAccountsUserAccessor, HttpContextCurrentAccountsUserAccessor>();
        return services;
    }
}

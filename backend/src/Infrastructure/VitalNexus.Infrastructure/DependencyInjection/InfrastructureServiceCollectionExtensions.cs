using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VitalNexus.Application.Abstractions;
using VitalNexus.Infrastructure.Configuration;
using VitalNexus.Infrastructure.Persistence.AccountBusiness;
using VitalNexus.Infrastructure.Security;

namespace VitalNexus.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public const string AccountsConnectionStringName = "Accounts";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(AccountsConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{AccountsConnectionStringName}' is not configured.");
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<AccountBusinessDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}

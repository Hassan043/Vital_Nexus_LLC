using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

internal static class AccountsUserProfileEnricher
{
    public static async Task<AccountsUser> EnrichAsync(
        AccountsUser user,
        IUserRoleRepository userRoleRepository,
        IClinicMembershipRepository clinicMembershipRepository,
        CancellationToken cancellationToken)
    {
        var roles = await userRoleRepository.GetRoleNamesForUserAsync(user.Id, cancellationToken);
        var memberships = await clinicMembershipRepository.GetMembershipsForUserAsync(user.Id, cancellationToken);

        return new AccountsUser
        {
            Id = user.Id,
            EntraObjectId = user.EntraObjectId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            Roles = roles,
            ClinicMemberships = memberships,
        };
    }
}

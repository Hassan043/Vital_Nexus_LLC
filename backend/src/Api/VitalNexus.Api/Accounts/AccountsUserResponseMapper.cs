using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Accounts;

internal static class AccountsUserResponseMapper
{
    public static object MapClinicMembership(ClinicMembership membership)
    {
        return new
        {
            clinicId = membership.ClinicId,
            clinicName = membership.ClinicName,
            joinedAt = membership.JoinedAt,
            isActive = membership.IsActive,
        };
    }

    public static object MapRolesAndMemberships(AccountsUser user)
    {
        return new
        {
            roles = user.Roles,
            clinicMemberships = user.ClinicMemberships.Select(MapClinicMembership).ToArray(),
        };
    }
}

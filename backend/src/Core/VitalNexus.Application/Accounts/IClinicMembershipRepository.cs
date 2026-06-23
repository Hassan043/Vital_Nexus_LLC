using VitalNexus.Domain.Accounts;

namespace VitalNexus.Application.Accounts;

public interface IClinicMembershipRepository
{
    Task<IReadOnlyList<ClinicMembership>> GetMembershipsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddMembershipAsync(
        Guid userId,
        ClinicMembership membership,
        CancellationToken cancellationToken = default);
}

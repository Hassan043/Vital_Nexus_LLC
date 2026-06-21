namespace VitalNexus.Application.Accounts;

public interface IUserRoleRepository
{
    Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AssignRoleAsync(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default);
}

using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlUserRoleRepository(IAccountsDbConnectionFactory connectionFactory) : IUserRoleRepository
{
    private static readonly HashSet<string> KnownRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationRoles.Admin,
        ApplicationRoles.User,
    };

    public async Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var roles = await connection.QueryAsync<string>(
            """
            SELECT ar.Name
            FROM dbo.UserRoles ur
            INNER JOIN dbo.ApplicationRoles ar ON ar.Id = ur.RoleId
            WHERE ur.UserId = @userId
            ORDER BY ar.Name
            """,
            new { userId });

        return roles.ToList();
    }

    public async Task AssignRoleAsync(
        Guid userId,
        Guid customerId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        if (!KnownRoles.Contains(roleName))
        {
            throw new InvalidOperationException($"Unknown application role '{roleName}'.");
        }

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        if (string.Equals(roleName, ApplicationRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var existingAdminId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                """
                SELECT TOP 1 u.Id
                FROM dbo.Users u
                INNER JOIN dbo.UserRoles ur ON ur.UserId = u.Id
                INNER JOIN dbo.ApplicationRoles ar ON ar.Id = ur.RoleId
                WHERE u.CustomerId = @customerId
                  AND ar.Name = @adminRole
                """,
                new { customerId, adminRole = ApplicationRoles.Admin });

            if (existingAdminId.HasValue && existingAdminId.Value != userId)
            {
                throw new InvalidOperationException(
                    "This customer already has an active Admin. Only one Admin is allowed per customer.");
            }
        }

        var roleId = await connection.QuerySingleAsync<int>(
            "SELECT Id FROM dbo.ApplicationRoles WHERE Name = @roleName",
            new { roleName });

        await connection.ExecuteAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @userId AND RoleId = @roleId)
            BEGIN
                INSERT INTO dbo.UserRoles (UserId, RoleId, AssignedAt)
                VALUES (@userId, @roleId, SYSUTCDATETIME())
            END
            """,
            new { userId, roleId });
    }

    public async Task RemoveAllRolesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            "DELETE FROM dbo.UserRoles WHERE UserId = @userId",
            new { userId });
    }
}

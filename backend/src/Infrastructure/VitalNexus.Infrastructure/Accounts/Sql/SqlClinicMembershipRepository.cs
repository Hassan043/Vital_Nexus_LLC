using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlClinicMembershipRepository(IAccountsDbConnectionFactory connectionFactory) : IClinicMembershipRepository
{
    public async Task<IReadOnlyList<ClinicMembership>> GetMembershipsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var memberships = await connection.QueryAsync<ClinicMembership>(
            """
            SELECT
                cm.ClinicId,
                c.Name AS ClinicName,
                cm.JoinedAt,
                cm.IsActive
            FROM dbo.ClinicMemberships cm
            INNER JOIN dbo.Clinics c ON c.Id = cm.ClinicId
            WHERE cm.UserId = @userId
            ORDER BY c.Name
            """,
            new { userId });

        return memberships.ToList();
    }

    public async Task AddMembershipAsync(
        Guid userId,
        ClinicMembership membership,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            IF NOT EXISTS (SELECT 1 FROM dbo.ClinicMemberships WHERE UserId = @userId AND ClinicId = @clinicId)
            BEGIN
                INSERT INTO dbo.ClinicMemberships (UserId, ClinicId, JoinedAt, IsActive)
                VALUES (@userId, @clinicId, @joinedAt, @isActive)
            END
            ELSE
            BEGIN
                UPDATE dbo.ClinicMemberships
                SET JoinedAt = @joinedAt,
                    IsActive = @isActive
                WHERE UserId = @userId AND ClinicId = @clinicId
            END
            """,
            new
            {
                userId,
                clinicId = membership.ClinicId,
                joinedAt = membership.JoinedAt,
                isActive = membership.IsActive,
            });
    }
}

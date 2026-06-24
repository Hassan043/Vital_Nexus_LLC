using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlUserInvitationRepository(IAccountsDbConnectionFactory connectionFactory) : IUserInvitationRepository
{
    public async Task<UserInvitation> CreateAsync(UserInvitation invitation, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.UserInvitations
                (Id, CustomerId, Email, RoleName, InvitedByUserId, CreatedAt, AcceptedAt)
            VALUES
                (@Id, @CustomerId, @Email, @RoleName, @InvitedByUserId, @CreatedAt, @AcceptedAt)
            """,
            new
            {
                invitation.Id,
                invitation.CustomerId,
                Email = invitation.Email.Trim(),
                invitation.RoleName,
                invitation.InvitedByUserId,
                invitation.CreatedAt,
                invitation.AcceptedAt,
            },
            transaction);

        foreach (var clinicId in invitation.ClinicIds)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.UserInvitationClinics (InvitationId, ClinicId)
                VALUES (@invitationId, @clinicId)
                """,
                new { invitationId = invitation.Id, clinicId },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
        return invitation;
    }

    public async Task<UserInvitation?> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<InvitationRow>(
            """
            SELECT Id, CustomerId, Email, RoleName, InvitedByUserId, CreatedAt, AcceptedAt
            FROM dbo.UserInvitations
            WHERE Email = @email AND AcceptedAt IS NULL
            """,
            new { email = email.Trim() });

        return row is null ? null : await MapInvitationAsync(connection, row);
    }

    public async Task<UserInvitation> MarkAcceptedAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var acceptedAt = DateTime.UtcNow;
        var updated = await connection.ExecuteAsync(
            """
            UPDATE dbo.UserInvitations
            SET AcceptedAt = @acceptedAt
            WHERE Id = @invitationId AND AcceptedAt IS NULL
            """,
            new { invitationId, acceptedAt });

        if (updated == 0)
        {
            throw new InvalidOperationException("Invitation was not found.");
        }

        var row = await connection.QuerySingleAsync<InvitationRow>(
            """
            SELECT Id, CustomerId, Email, RoleName, InvitedByUserId, CreatedAt, AcceptedAt
            FROM dbo.UserInvitations
            WHERE Id = @invitationId
            """,
            new { invitationId });

        return await MapInvitationAsync(connection, row);
    }

    public async Task<IReadOnlyList<UserInvitation>> GetPendingByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<InvitationRow>(
            """
            SELECT Id, CustomerId, Email, RoleName, InvitedByUserId, CreatedAt, AcceptedAt
            FROM dbo.UserInvitations
            WHERE CustomerId = @customerId AND AcceptedAt IS NULL
            ORDER BY Email
            """,
            new { customerId });

        var invitations = new List<UserInvitation>();
        foreach (var row in rows)
        {
            invitations.Add(await MapInvitationAsync(connection, row));
        }

        return invitations;
    }

    private static async Task<UserInvitation> MapInvitationAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        InvitationRow row)
    {
        var clinicIds = await connection.QueryAsync<Guid>(
            """
            SELECT ClinicId
            FROM dbo.UserInvitationClinics
            WHERE InvitationId = @invitationId
            ORDER BY ClinicId
            """,
            new { invitationId = row.Id });

        return new UserInvitation
        {
            Id = row.Id,
            CustomerId = row.CustomerId,
            Email = row.Email,
            RoleName = row.RoleName,
            InvitedByUserId = row.InvitedByUserId,
            ClinicIds = clinicIds.ToList(),
            CreatedAt = row.CreatedAt,
            AcceptedAt = row.AcceptedAt,
        };
    }

    private sealed class InvitationRow
    {
        public Guid Id { get; init; }

        public Guid CustomerId { get; init; }

        public string Email { get; init; } = string.Empty;

        public string RoleName { get; init; } = ApplicationRoles.User;

        public Guid InvitedByUserId { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? AcceptedAt { get; init; }
    }
}

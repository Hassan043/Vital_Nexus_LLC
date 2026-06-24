using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlAccountsUserRepository(IAccountsDbConnectionFactory connectionFactory) : IAccountsUserRepository
{
    public async Task<AccountsUser?> GetByEntraObjectIdAsync(
        Guid entraObjectId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await QueryUserAsync(
            connection,
            """
            SELECT Id, EntraObjectId, CustomerId, Email, DisplayName, AccountStatus, CreatedAt
            FROM dbo.Users
            WHERE EntraObjectId = @entraObjectId
            """,
            new { entraObjectId });
    }

    public async Task<AccountsUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await QueryUserAsync(
            connection,
            """
            SELECT Id, EntraObjectId, CustomerId, Email, DisplayName, AccountStatus, CreatedAt
            FROM dbo.Users
            WHERE Email = @email
            """,
            new { email = email.Trim() });
    }

    public async Task<AccountsUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await QueryUserAsync(
            connection,
            """
            SELECT Id, EntraObjectId, CustomerId, Email, DisplayName, AccountStatus, CreatedAt
            FROM dbo.Users
            WHERE Id = @userId
            """,
            new { userId });
    }

    public async Task<IReadOnlyList<AccountsUser>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var users = await connection.QueryAsync<AccountsUserRow>(
            """
            SELECT Id, EntraObjectId, CustomerId, Email, DisplayName, AccountStatus, CreatedAt
            FROM dbo.Users
            WHERE CustomerId = @customerId
            ORDER BY Email
            """,
            new { customerId });

        return users.Select(MapRow).ToList();
    }

    public async Task<AccountsUser> CreateAsync(AccountsUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Users
                (Id, EntraObjectId, CustomerId, Email, DisplayName, AccountStatus, CreatedAt)
            VALUES
                (@Id, @EntraObjectId, @CustomerId, @Email, @DisplayName, @AccountStatus, @CreatedAt)
            """,
            new
            {
                user.Id,
                user.EntraObjectId,
                user.CustomerId,
                Email = user.Email.Trim(),
                user.DisplayName,
                user.AccountStatus,
                user.CreatedAt,
            });

        return MapRow(new AccountsUserRow
        {
            Id = user.Id,
            EntraObjectId = user.EntraObjectId,
            CustomerId = user.CustomerId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AccountStatus = user.AccountStatus,
            CreatedAt = user.CreatedAt,
        });
    }

    public async Task<AccountsUser> UpdateAsync(AccountsUser user, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE dbo.Users
            SET EntraObjectId = @EntraObjectId,
                CustomerId = @CustomerId,
                Email = @Email,
                DisplayName = @DisplayName,
                AccountStatus = @AccountStatus
            WHERE Id = @Id
            """,
            new
            {
                user.Id,
                user.EntraObjectId,
                user.CustomerId,
                Email = user.Email.Trim(),
                user.DisplayName,
                user.AccountStatus,
            });

        return MapRow(new AccountsUserRow
        {
            Id = user.Id,
            EntraObjectId = user.EntraObjectId,
            CustomerId = user.CustomerId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AccountStatus = user.AccountStatus,
            CreatedAt = user.CreatedAt,
        });
    }

    private static async Task<AccountsUser?> QueryUserAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        string sql,
        object parameters)
    {
        var row = await connection.QuerySingleOrDefaultAsync<AccountsUserRow>(sql, parameters);
        return row is null ? null : MapRow(row);
    }

    private static AccountsUser MapRow(AccountsUserRow row) =>
        new()
        {
            Id = row.Id,
            EntraObjectId = row.EntraObjectId,
            CustomerId = row.CustomerId,
            Email = row.Email,
            DisplayName = row.DisplayName,
            AccountStatus = row.AccountStatus,
            CreatedAt = row.CreatedAt,
        };

    private sealed class AccountsUserRow
    {
        public Guid Id { get; init; }

        public Guid? EntraObjectId { get; init; }

        public Guid CustomerId { get; init; }

        public string Email { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string AccountStatus { get; init; } = AccountStatuses.Active;

        public DateTime CreatedAt { get; init; }
    }
}

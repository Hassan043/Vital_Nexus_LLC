using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlCustomerPatientsDatabaseRepository(IAccountsDbConnectionFactory connectionFactory)
    : ICustomerPatientsDatabaseRepository
{
    public async Task<CustomerPatientsDatabase?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CustomerPatientsDatabase>(
            """
            SELECT CustomerId, DatabaseName, ServerName, IsActive, ProvisionedAt
            FROM dbo.CustomerPatientsDatabases
            WHERE CustomerId = @customerId
            """,
            new { customerId });
    }

    public async Task<CustomerPatientsDatabase> CreateAsync(
        CustomerPatientsDatabase routing,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.CustomerPatientsDatabases
                (CustomerId, DatabaseName, ServerName, IsActive, ProvisionedAt)
            VALUES
                (@CustomerId, @DatabaseName, @ServerName, @IsActive, @ProvisionedAt)
            """,
            routing);

        return routing;
    }

    public async Task UpsertAsync(
        CustomerPatientsDatabase routing,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            MERGE dbo.CustomerPatientsDatabases AS target
            USING (SELECT @CustomerId AS CustomerId) AS source
            ON target.CustomerId = source.CustomerId
            WHEN MATCHED THEN
                UPDATE SET
                    DatabaseName = @DatabaseName,
                    ServerName = @ServerName,
                    IsActive = @IsActive,
                    ProvisionedAt = @ProvisionedAt
            WHEN NOT MATCHED THEN
                INSERT (CustomerId, DatabaseName, ServerName, IsActive, ProvisionedAt)
                VALUES (@CustomerId, @DatabaseName, @ServerName, @IsActive, @ProvisionedAt);
            """,
            routing);
    }
}

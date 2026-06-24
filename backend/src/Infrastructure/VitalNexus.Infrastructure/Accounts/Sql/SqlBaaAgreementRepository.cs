using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlBaaAgreementRepository(IAccountsDbConnectionFactory connectionFactory) : IBaaAgreementRepository
{
    public async Task<BaaAgreement?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<BaaAgreement>(
            """
            SELECT CustomerId, SignedByUserId, SignedAt, AgreementVersion
            FROM dbo.BaaAgreements
            WHERE CustomerId = @customerId
            """,
            new { customerId });
    }

    public async Task<BaaAgreement> SignAsync(BaaAgreement agreement, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            MERGE dbo.BaaAgreements AS target
            USING (SELECT @CustomerId AS CustomerId) AS source
            ON target.CustomerId = source.CustomerId
            WHEN MATCHED THEN
                UPDATE SET
                    SignedByUserId = @SignedByUserId,
                    SignedAt = @SignedAt,
                    AgreementVersion = @AgreementVersion
            WHEN NOT MATCHED THEN
                INSERT (CustomerId, SignedByUserId, SignedAt, AgreementVersion)
                VALUES (@CustomerId, @SignedByUserId, @SignedAt, @AgreementVersion);
            """,
            agreement);

        return agreement;
    }
}

using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlSubscriptionRepository(IAccountsDbConnectionFactory connectionFactory) : ISubscriptionRepository
{
    public async Task<Subscription> CreateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Subscriptions
                (CustomerId, PlanTierId, Status, CreatedAt, ActivatedAt)
            VALUES
                (@CustomerId, @PlanTierId, @Status, @CreatedAt, @ActivatedAt)
            """,
            subscription);

        return subscription;
    }

    public async Task<Subscription?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Subscription>(
            """
            SELECT CustomerId, PlanTierId, Status, CreatedAt, ActivatedAt
            FROM dbo.Subscriptions
            WHERE CustomerId = @customerId
            """,
            new { customerId });
    }
}

using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlOnboardingAuditRepository(IAccountsDbConnectionFactory connectionFactory) : IOnboardingAuditRepository
{
    public async Task RecordAsync(OnboardingAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.OnboardingAuditEvents
                (Id, CustomerId, ActorUserId, EventType, Detail, OccurredAt)
            VALUES
                (@Id, @CustomerId, @ActorUserId, @EventType, @Detail, @OccurredAt)
            """,
            auditEvent);
    }

    public async Task<IReadOnlyList<OnboardingAuditEvent>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<OnboardingAuditEvent>(
            """
            SELECT Id, CustomerId, ActorUserId, EventType, Detail, OccurredAt
            FROM dbo.OnboardingAuditEvents
            WHERE CustomerId = @customerId
            ORDER BY OccurredAt
            """,
            new { customerId });

        return events.ToList();
    }
}

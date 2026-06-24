using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlPlanTierRepository(IAccountsDbConnectionFactory connectionFactory) : IPlanTierRepository
{
    public async Task<PlanTier?> GetByIdAsync(int planTierId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PlanTier>(
            """
            SELECT Id, Name, Description, MonthlyPriceCents, PatientCapMax, IsActive
            FROM dbo.PlanTiers
            WHERE Id = @planTierId
            """,
            new { planTierId });
    }

    public async Task<IReadOnlyList<PlanTier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var tiers = await connection.QueryAsync<PlanTier>(
            """
            SELECT Id, Name, Description, MonthlyPriceCents, PatientCapMax, IsActive
            FROM dbo.PlanTiers
            WHERE IsActive = 1
            ORDER BY Id
            """);

        return tiers.ToList();
    }

    public async Task<PlanTier> UpsertAsync(PlanTier planTier, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            MERGE dbo.PlanTiers AS target
            USING (SELECT @Id AS Id) AS source
            ON target.Id = source.Id
            WHEN MATCHED THEN
                UPDATE SET
                    Name = @Name,
                    Description = @Description,
                    MonthlyPriceCents = @MonthlyPriceCents,
                    PatientCapMax = @PatientCapMax,
                    IsActive = @IsActive
            WHEN NOT MATCHED THEN
                INSERT (Id, Name, Description, MonthlyPriceCents, PatientCapMax, IsActive)
                VALUES (@Id, @Name, @Description, @MonthlyPriceCents, @PatientCapMax, @IsActive);
            """,
            planTier);

        return planTier;
    }
}

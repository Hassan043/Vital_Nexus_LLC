using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlCustomerOnboardingStateRepository(IAccountsDbConnectionFactory connectionFactory)
    : ICustomerOnboardingStateRepository
{
    public async Task<CustomerOnboardingState?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CustomerOnboardingState>(
            """
            SELECT
                CustomerId,
                SelectedPlanTierId,
                ClinicProfileComplete,
                ProvisioningCompletedAt,
                UpdatedAt
            FROM dbo.CustomerOnboardingStates
            WHERE CustomerId = @customerId
            """,
            new { customerId });
    }

    public async Task<CustomerOnboardingState> UpsertAsync(
        CustomerOnboardingState state,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            MERGE dbo.CustomerOnboardingStates AS target
            USING (SELECT @CustomerId AS CustomerId) AS source
            ON target.CustomerId = source.CustomerId
            WHEN MATCHED THEN
                UPDATE SET
                    SelectedPlanTierId = @SelectedPlanTierId,
                    ClinicProfileComplete = @ClinicProfileComplete,
                    ProvisioningCompletedAt = @ProvisioningCompletedAt,
                    UpdatedAt = @UpdatedAt
            WHEN NOT MATCHED THEN
                INSERT (CustomerId, SelectedPlanTierId, ClinicProfileComplete, ProvisioningCompletedAt, UpdatedAt)
                VALUES (@CustomerId, @SelectedPlanTierId, @ClinicProfileComplete, @ProvisioningCompletedAt, @UpdatedAt);
            """,
            state);

        return state;
    }
}

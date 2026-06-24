using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlClinicProfileRepository(IAccountsDbConnectionFactory connectionFactory) : IClinicProfileRepository
{
    public async Task<ClinicProfile> CreateAsync(ClinicProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.ClinicProfiles
                (ClinicId, DisplayName, ContactEmail, Phone, TimeZoneId, CreatedAt, UpdatedAt)
            VALUES
                (@ClinicId, @DisplayName, @ContactEmail, @Phone, @TimeZoneId, @CreatedAt, @UpdatedAt)
            """,
            profile);

        return profile;
    }

    public async Task<ClinicProfile?> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClinicProfile>(
            """
            SELECT ClinicId, DisplayName, ContactEmail, Phone, TimeZoneId, CreatedAt, UpdatedAt
            FROM dbo.ClinicProfiles
            WHERE ClinicId = @clinicId
            """,
            new { clinicId });
    }

    public async Task<ClinicProfile> UpdateAsync(ClinicProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE dbo.ClinicProfiles
            SET DisplayName = @DisplayName,
                ContactEmail = @ContactEmail,
                Phone = @Phone,
                TimeZoneId = @TimeZoneId,
                UpdatedAt = @UpdatedAt
            WHERE ClinicId = @ClinicId
            """,
            profile);

        return profile;
    }
}

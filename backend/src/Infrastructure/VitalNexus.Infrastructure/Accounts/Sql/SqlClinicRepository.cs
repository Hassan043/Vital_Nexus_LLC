using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlClinicRepository(IAccountsDbConnectionFactory connectionFactory) : IClinicRepository
{
    public async Task<Clinic> CreateAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Clinics (Id, CustomerId, Name, CreatedAt, IsActive)
            VALUES (@Id, @CustomerId, @Name, @CreatedAt, @IsActive)
            """,
            new
            {
                clinic.Id,
                clinic.CustomerId,
                clinic.Name,
                clinic.CreatedAt,
                clinic.IsActive,
            });

        return clinic;
    }

    public async Task<Clinic?> GetByIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Clinic>(
            """
            SELECT Id, CustomerId, Name, CreatedAt, IsActive
            FROM dbo.Clinics
            WHERE Id = @clinicId
            """,
            new { clinicId });
    }

    public async Task<IReadOnlyList<Clinic>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var clinics = await connection.QueryAsync<Clinic>(
            """
            SELECT Id, CustomerId, Name, CreatedAt, IsActive
            FROM dbo.Clinics
            WHERE CustomerId = @customerId
            ORDER BY Name
            """,
            new { customerId });

        return clinics.ToList();
    }
}

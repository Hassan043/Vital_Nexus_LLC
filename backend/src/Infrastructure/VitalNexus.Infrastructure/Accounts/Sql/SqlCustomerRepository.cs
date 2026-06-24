using Dapper;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public sealed class SqlCustomerRepository(IAccountsDbConnectionFactory connectionFactory) : ICustomerRepository
{
    public async Task<Customer> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Customers (Id, Name, CreatedAt)
            VALUES (@Id, @Name, @CreatedAt)
            """,
            new { customer.Id, customer.Name, customer.CreatedAt });

        return customer;
    }

    public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Customer>(
            """
            SELECT Id, Name, CreatedAt
            FROM dbo.Customers
            WHERE Id = @customerId
            """,
            new { customerId });
    }

    public async Task<Customer> UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            """
            UPDATE dbo.Customers
            SET Name = @Name
            WHERE Id = @Id
            """,
            new { customer.Id, customer.Name });

        return customer;
    }
}

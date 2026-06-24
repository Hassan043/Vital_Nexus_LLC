using Microsoft.Data.SqlClient;

namespace VitalNexus.Infrastructure.Accounts.Sql;

public interface IAccountsDbConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class AccountsDbConnectionFactory(string connectionString) : IAccountsDbConnectionFactory
{
    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

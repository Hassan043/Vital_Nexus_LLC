using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class PatientsDatabaseConnectionStringFactory
{
    private readonly IConfiguration _configuration;
    private readonly CustomerPatientsDatabaseOptions _options;

    public PatientsDatabaseConnectionStringFactory(
        IConfiguration configuration,
        IOptions<CustomerPatientsDatabaseOptions> options)
    {
        _configuration = configuration;
        _options = options.Value;
    }

    public string Build(CustomerPatientsDatabase routing)
    {
        var template = _configuration.GetConnectionString(_options.TemplateConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{_options.TemplateConnectionStringName}' is not configured.");

        var builder = new SqlConnectionStringBuilder(template)
        {
            InitialCatalog = routing.DatabaseName,
        };

        if (!string.IsNullOrWhiteSpace(routing.ServerName))
        {
            builder.DataSource = routing.ServerName;
        }

        return builder.ConnectionString;
    }
}

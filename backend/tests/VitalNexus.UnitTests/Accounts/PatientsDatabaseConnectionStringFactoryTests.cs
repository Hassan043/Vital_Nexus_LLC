using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class PatientsDatabaseConnectionStringFactoryTests
{
    [Fact]
    public void Build_ReplacesInitialCatalogWithCustomerDatabaseName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PatientHealth"] =
                    "Server=sql-vnx-phi-dev.database.windows.net;Database=PatientHealth;User ID=app;Password=secret;",
            })
            .Build();

        var factory = new PatientsDatabaseConnectionStringFactory(
            configuration,
            Options.Create(new CustomerPatientsDatabaseOptions()));

        var connectionString = factory.Build(new CustomerPatientsDatabase
        {
            CustomerId = Guid.NewGuid(),
            DatabaseName = "Patients-AcmeCustomer",
        });

        Assert.Contains("Initial Catalog=Patients-AcmeCustomer", connectionString);
        Assert.Contains("Data Source=sql-vnx-phi-dev.database.windows.net", connectionString);
    }

    [Fact]
    public void Build_OverridesServerWhenRoutingSpecifiesServerName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PatientHealth"] =
                    "Server=sql-vnx-phi-dev.database.windows.net;Database=PatientHealth;User ID=app;Password=secret;",
            })
            .Build();

        var factory = new PatientsDatabaseConnectionStringFactory(
            configuration,
            Options.Create(new CustomerPatientsDatabaseOptions()));

        var connectionString = factory.Build(new CustomerPatientsDatabase
        {
            CustomerId = Guid.NewGuid(),
            DatabaseName = "Patients-AcmeCustomer",
            ServerName = "sql-vnx-phi-prod.database.windows.net",
        });

        Assert.Contains("Data Source=sql-vnx-phi-prod.database.windows.net", connectionString);
    }
}

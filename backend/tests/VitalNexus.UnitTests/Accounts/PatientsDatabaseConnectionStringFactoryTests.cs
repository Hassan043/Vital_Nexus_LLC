using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class PatientsDatabaseConnectionStringFactoryTests
{
    [Fact]
    public void Build_ReplacesInitialCatalogWithClinicDatabaseName()
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
            Options.Create(new ClinicPatientsDatabaseOptions()));

        var connectionString = factory.Build(new ClinicPatientsDatabase
        {
            ClinicId = Guid.NewGuid(),
            DatabaseName = "Patients-AcmeClinic",
        });

        Assert.Contains("Initial Catalog=Patients-AcmeClinic", connectionString);
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
            Options.Create(new ClinicPatientsDatabaseOptions()));

        var connectionString = factory.Build(new ClinicPatientsDatabase
        {
            ClinicId = Guid.NewGuid(),
            DatabaseName = "Patients-AcmeClinic",
            ServerName = "sql-vnx-phi-prod.database.windows.net",
        });

        Assert.Contains("Data Source=sql-vnx-phi-prod.database.windows.net", connectionString);
    }
}

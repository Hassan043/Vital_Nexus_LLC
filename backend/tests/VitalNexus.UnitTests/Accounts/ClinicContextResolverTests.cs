using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class ClinicContextResolverTests
{
    private static readonly Guid ClinicOneId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ClinicTwoId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task ResolveAsync_ReturnsNullWhenUserHasNoActiveMemberships()
    {
        var resolver = CreateResolver();

        var context = await resolver.ResolveAsync(CreateUser([]));

        Assert.Null(context);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsContextForSingleActiveMembership()
    {
        var resolver = CreateResolver();
        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
        ]);

        var context = await resolver.ResolveAsync(user);

        Assert.NotNull(context);
        Assert.Equal(ClinicOneId, context!.ClinicId);
        Assert.Equal("Clinic One", context.ClinicName);
        Assert.Equal("Patients-ClinicOne", context.PatientsDatabaseName);
        Assert.Contains("Initial Catalog=Patients-ClinicOne", context.PatientsConnectionString);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNullWhenMultipleMembershipsWithoutRequestedClinic()
    {
        var resolver = CreateResolver();
        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
            CreateMembership(ClinicTwoId, "Clinic Two"),
        ]);

        var context = await resolver.ResolveAsync(user);

        Assert.Null(context);
    }

    [Fact]
    public async Task ResolveAsync_UsesRequestedClinicWhenUserHasMultipleMemberships()
    {
        var resolver = CreateResolver();
        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
            CreateMembership(ClinicTwoId, "Clinic Two"),
        ]);

        var context = await resolver.ResolveAsync(user, ClinicTwoId);

        Assert.NotNull(context);
        Assert.Equal(ClinicTwoId, context!.ClinicId);
        Assert.Equal("Patients-ClinicTwo", context.PatientsDatabaseName);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNullWhenRequestedClinicIsNotAuthorized()
    {
        var resolver = CreateResolver();
        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
        ]);

        var context = await resolver.ResolveAsync(user, ClinicTwoId);

        Assert.Null(context);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNullWhenRoutingIsMissing()
    {
        var resolver = CreateResolver(includeRouting: false);
        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
        ]);

        var context = await resolver.ResolveAsync(user);

        Assert.Null(context);
    }

    [Fact]
    public async Task ResolveAsync_ExcludesInactiveMemberships()
    {
        var resolver = CreateResolver();
        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One", isActive: false),
        ]);

        var context = await resolver.ResolveAsync(user);

        Assert.Null(context);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNullWhenRoutingIsInactive()
    {
        var repository = new InMemoryClinicPatientsDatabaseRepository(
            Options.Create(new ClinicPatientsDatabaseOptions()));
        await repository.AddRoutingAsync(new ClinicPatientsDatabase
        {
            ClinicId = ClinicOneId,
            DatabaseName = "Patients-ClinicOne",
            IsActive = false,
        });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PatientHealth"] =
                    "Server=sql-vnx-phi-dev.database.windows.net;Database=PatientHealth;User ID=app;Password=secret;",
            })
            .Build();

        var resolver = new ClinicContextResolver(
            repository,
            new PatientsDatabaseConnectionStringFactory(
                configuration,
                Options.Create(new ClinicPatientsDatabaseOptions())));

        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
        ]);

        var context = await resolver.ResolveAsync(user);

        Assert.Null(context);
    }

    private static ClinicContextResolver CreateResolver(bool includeRouting = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PatientHealth"] =
                    "Server=sql-vnx-phi-dev.database.windows.net;Database=PatientHealth;User ID=app;Password=secret;",
            })
            .Build();

        var repository = new InMemoryClinicPatientsDatabaseRepository(
            Options.Create(new ClinicPatientsDatabaseOptions()));

        if (includeRouting)
        {
            repository.AddRoutingAsync(new ClinicPatientsDatabase
            {
                ClinicId = ClinicOneId,
                DatabaseName = "Patients-ClinicOne",
                IsActive = true,
            }).GetAwaiter().GetResult();
            repository.AddRoutingAsync(new ClinicPatientsDatabase
            {
                ClinicId = ClinicTwoId,
                DatabaseName = "Patients-ClinicTwo",
                IsActive = true,
            }).GetAwaiter().GetResult();
        }

        return new ClinicContextResolver(
            repository,
            new PatientsDatabaseConnectionStringFactory(
                configuration,
                Options.Create(new ClinicPatientsDatabaseOptions())));
    }

    private static AccountsUser CreateUser(IReadOnlyList<ClinicMembership> memberships) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntraObjectId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Email = "clinician@example.com",
            ClinicMemberships = memberships,
        };

    private static ClinicMembership CreateMembership(
        Guid clinicId,
        string clinicName,
        bool isActive = true) =>
        new()
        {
            ClinicId = clinicId,
            ClinicName = clinicName,
            JoinedAt = DateTime.UtcNow,
            IsActive = isActive,
        };
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class ClinicContextResolverTests
{
    private static readonly Guid CustomerId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
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
        Assert.Equal("Patients-CustomerDemo", context.PatientsDatabaseName);
        Assert.Contains("Initial Catalog=Patients-CustomerDemo", context.PatientsConnectionString);
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
        Assert.Equal("Patients-CustomerDemo", context.PatientsDatabaseName);
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
        var repository = new InMemoryCustomerPatientsDatabaseRepository();
        await repository.CreateAsync(new CustomerPatientsDatabase
        {
            CustomerId = CustomerId,
            DatabaseName = "Patients-CustomerDemo",
            IsActive = false,
            ProvisionedAt = DateTime.UtcNow,
        });

        var resolver = CreateResolver(repository);

        var user = CreateUser([
            CreateMembership(ClinicOneId, "Clinic One"),
        ]);

        var context = await resolver.ResolveAsync(user);

        Assert.Null(context);
    }

    private static ClinicContextResolver CreateResolver(bool includeRouting = true)
    {
        var repository = new InMemoryCustomerPatientsDatabaseRepository();
        if (includeRouting)
        {
            repository.CreateAsync(new CustomerPatientsDatabase
            {
                CustomerId = CustomerId,
                DatabaseName = "Patients-CustomerDemo",
                IsActive = true,
                ProvisionedAt = DateTime.UtcNow,
            }).GetAwaiter().GetResult();
        }

        return CreateResolver(repository);
    }

    private static ClinicContextResolver CreateResolver(InMemoryCustomerPatientsDatabaseRepository repository)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PatientHealth"] =
                    "Server=sql-vnx-phi-dev.database.windows.net;Database=PatientHealth;User ID=app;Password=secret;",
            })
            .Build();

        return new ClinicContextResolver(
            repository,
            new PatientsDatabaseConnectionStringFactory(
                configuration,
                Options.Create(new CustomerPatientsDatabaseOptions())));
    }

    private static AccountsUser CreateUser(IReadOnlyList<ClinicMembership> memberships) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntraObjectId = Guid.NewGuid(),
            CustomerId = CustomerId,
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

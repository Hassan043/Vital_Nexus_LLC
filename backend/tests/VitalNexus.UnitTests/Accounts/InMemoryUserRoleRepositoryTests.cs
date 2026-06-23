using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class InMemoryUserRoleRepositoryTests
{
    [Fact]
    public async Task AssignRoleAsync_PersistsRoleForUser()
    {
        var repository = new InMemoryUserRoleRepository();
        var userId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await repository.AssignRoleAsync(userId, customerId, ApplicationRoles.User);
        var roles = await repository.GetRoleNamesForUserAsync(userId);

        Assert.Single(roles);
        Assert.Equal(ApplicationRoles.User, roles[0]);
    }

    [Fact]
    public async Task AssignRoleAsync_ThrowsForUnknownRole()
    {
        var repository = new InMemoryUserRoleRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AssignRoleAsync(Guid.NewGuid(), Guid.NewGuid(), "UnknownRole"));
    }

    [Fact]
    public async Task AssignRoleAsync_AllowsOnlyOneAdminPerCustomer()
    {
        var repository = new InMemoryUserRoleRepository();
        var customerId = Guid.NewGuid();
        var firstAdminId = Guid.NewGuid();
        var secondAdminId = Guid.NewGuid();

        await repository.AssignRoleAsync(firstAdminId, customerId, ApplicationRoles.Admin);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AssignRoleAsync(secondAdminId, customerId, ApplicationRoles.Admin));

        Assert.Contains("one Admin", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

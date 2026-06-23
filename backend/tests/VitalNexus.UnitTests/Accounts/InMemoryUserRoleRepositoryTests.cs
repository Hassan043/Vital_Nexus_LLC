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

        await repository.AssignRoleAsync(userId, ApplicationRoles.Clinician);
        var roles = await repository.GetRoleNamesForUserAsync(userId);

        Assert.Single(roles);
        Assert.Equal(ApplicationRoles.Clinician, roles[0]);
    }

    [Fact]
    public async Task AssignRoleAsync_ThrowsForUnknownRole()
    {
        var repository = new InMemoryUserRoleRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.AssignRoleAsync(Guid.NewGuid(), "UnknownRole"));
    }
}

using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class ExternalIdentityAccountsUserMapperTests
{
    [Fact]
    public async Task MapAsync_CreatesInternalAccountsUserForNewExternalIdentity()
    {
        var repository = new InMemoryAccountsUserRepository();
        var roleRepository = new InMemoryUserRoleRepository();
        var mapper = new ExternalIdentityAccountsUserMapper(repository, roleRepository);
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000099",
            Email = "clinician@example.com",
            DisplayName = "Test Clinician",
        };

        var user = await mapper.MapAsync(identity);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal(Guid.Parse(identity.ObjectId), user.EntraObjectId);
        Assert.Equal("clinician@example.com", user.Email);
        Assert.Equal("Test Clinician", user.DisplayName);
    }

    [Fact]
    public async Task MapAsync_AssignsDefaultClinicianRoleForNewExternalIdentity()
    {
        var repository = new InMemoryAccountsUserRepository();
        var roleRepository = new InMemoryUserRoleRepository();
        var mapper = new ExternalIdentityAccountsUserMapper(repository, roleRepository);
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000088",
            Email = "new-clinician@example.com",
            DisplayName = "New Clinician",
        };

        var user = await mapper.MapAsync(identity);
        var roles = await roleRepository.GetRoleNamesForUserAsync(user.Id);

        Assert.Single(roles);
        Assert.Equal(ApplicationRoles.Clinician, roles[0]);
    }

    [Fact]
    public async Task MapAsync_ReturnsExistingUserForSameEntraObjectId()
    {
        var repository = new InMemoryAccountsUserRepository();
        var roleRepository = new InMemoryUserRoleRepository();
        var mapper = new ExternalIdentityAccountsUserMapper(repository, roleRepository);
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000099",
            Email = "clinician@example.com",
            DisplayName = "Test Clinician",
        };

        var first = await mapper.MapAsync(identity);
        var second = await mapper.MapAsync(identity);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.EntraObjectId, second.EntraObjectId);
    }

    [Fact]
    public async Task MapAsync_UpdatesDisplayNameWhenExternalIdentityChanges()
    {
        var repository = new InMemoryAccountsUserRepository();
        var roleRepository = new InMemoryUserRoleRepository();
        var mapper = new ExternalIdentityAccountsUserMapper(repository, roleRepository);
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000099",
            Email = "clinician@example.com",
            DisplayName = "Test Clinician",
        };

        await mapper.MapAsync(identity);

        var updatedIdentity = identity with { DisplayName = "Updated Clinician" };
        var user = await mapper.MapAsync(updatedIdentity);

        Assert.Equal("Updated Clinician", user.DisplayName);
    }

    [Fact]
    public async Task MapAsync_ThrowsWhenEmailIsMissingForNewUser()
    {
        var repository = new InMemoryAccountsUserRepository();
        var roleRepository = new InMemoryUserRoleRepository();
        var mapper = new ExternalIdentityAccountsUserMapper(repository, roleRepository);
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000099",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => mapper.MapAsync(identity));
    }
}

using VitalNexus.Application.Accounts;
using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Accounts;

namespace VitalNexus.UnitTests.Accounts;

public sealed class ExternalIdentityAccountsUserMapperTests
{
    [Fact]
    public async Task MapAsync_CreatesInternalAccountsUserForNewExternalIdentity()
    {
        var mapper = CreateMapper();
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000099",
            Email = "clinician@example.com",
            DisplayName = "Test Clinician",
        };

        var user = await mapper.MapAsync(identity);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.NotEqual(Guid.Empty, user.CustomerId);
        Assert.Equal(Guid.Parse(identity.ObjectId), user.EntraObjectId);
        Assert.Equal("clinician@example.com", user.Email);
        Assert.Equal("Test Clinician", user.DisplayName);
    }

    [Fact]
    public async Task MapAsync_AssignsAdminRoleForNewCustomerRegistration()
    {
        var roleRepository = new InMemoryUserRoleRepository();
        var mapper = CreateMapper(roleRepository: roleRepository);
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000088",
            Email = "new-admin@example.com",
            DisplayName = "New Admin",
        };

        var user = await mapper.MapAsync(identity);
        var roles = await roleRepository.GetRoleNamesForUserAsync(user.Id);

        Assert.Single(roles);
        Assert.Equal(ApplicationRoles.Admin, roles[0]);
    }

    [Fact]
    public async Task MapAsync_ReturnsExistingUserForSameEntraObjectId()
    {
        var mapper = CreateMapper();
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
        var mapper = CreateMapper();
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
        var mapper = CreateMapper();
        var identity = new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000099",
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => mapper.MapAsync(identity));
    }

    [Fact]
    public async Task MapAsync_AcceptsInvitationAsUserRoleForExistingCustomer()
    {
        var repository = new InMemoryAccountsUserRepository();
        var customerRepository = new InMemoryCustomerRepository();
        var roleRepository = new InMemoryUserRoleRepository();
        var invitationRepository = new InMemoryUserInvitationRepository();
        var membershipRepository = new InMemoryClinicMembershipRepository();
        var customerId = Guid.NewGuid();
        await customerRepository.CreateAsync(new Customer
        {
            Id = customerId,
            Name = "Existing Customer",
            CreatedAt = DateTime.UtcNow,
        });

        var adminId = Guid.NewGuid();
        await repository.CreateAsync(new AccountsUser
        {
            Id = adminId,
            EntraObjectId = Guid.NewGuid(),
            CustomerId = customerId,
            Email = "admin@example.com",
            CreatedAt = DateTime.UtcNow,
        });
        await roleRepository.AssignRoleAsync(adminId, customerId, ApplicationRoles.Admin);
        var clinicId = Guid.NewGuid();
        await membershipRepository.AddMembershipAsync(
            adminId,
            new ClinicMembership
            {
                ClinicId = clinicId,
                ClinicName = "Main Clinic",
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
            });
        await invitationRepository.CreateAsync(new UserInvitation
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Email = "staff@example.com",
            RoleName = ApplicationRoles.User,
            InvitedByUserId = adminId,
            CreatedAt = DateTime.UtcNow,
        });

        var mapper = CreateMapper(
            repository,
            customerRepository,
            roleRepository,
            invitationRepository,
            membershipRepository);
        var user = await mapper.MapAsync(new TrustedExternalIdentity
        {
            ObjectId = "00000000-0000-4000-8000-000000000055",
            Email = "staff@example.com",
            DisplayName = "Staff User",
        });

        var roles = await roleRepository.GetRoleNamesForUserAsync(user.Id);
        Assert.Equal(customerId, user.CustomerId);
        Assert.Equal(ApplicationRoles.User, roles[0]);
    }

    private static ExternalIdentityAccountsUserMapper CreateMapper(
        InMemoryAccountsUserRepository? repository = null,
        InMemoryCustomerRepository? customerRepository = null,
        InMemoryUserRoleRepository? roleRepository = null,
        InMemoryUserInvitationRepository? invitationRepository = null,
        InMemoryClinicMembershipRepository? membershipRepository = null)
    {
        repository ??= new InMemoryAccountsUserRepository();
        customerRepository ??= new InMemoryCustomerRepository();
        roleRepository ??= new InMemoryUserRoleRepository();
        invitationRepository ??= new InMemoryUserInvitationRepository();
        membershipRepository ??= new InMemoryClinicMembershipRepository();

        var clinicRepository = new InMemoryClinicRepository();
        var clinicProfileRepository = new InMemoryClinicProfileRepository();
        var subscriptionRepository = new InMemorySubscriptionRepository();
        var patientsDatabaseRepository = new InMemoryCustomerPatientsDatabaseRepository();
        var onboardingService = new CustomerOnboardingService(
            customerRepository,
            repository,
            roleRepository,
            subscriptionRepository,
            clinicRepository,
            clinicProfileRepository,
            membershipRepository,
            patientsDatabaseRepository,
            new SimulatedPatientsDatabaseProvisioningService(
                patientsDatabaseRepository,
                Microsoft.Extensions.Options.Options.Create(new CustomerPatientsDatabaseOptions())));

        return new ExternalIdentityAccountsUserMapper(
            repository,
            customerRepository,
            roleRepository,
            invitationRepository,
            membershipRepository,
            onboardingService);
    }
}

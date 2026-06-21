using Microsoft.AspNetCore.Authorization;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;
using VitalNexus.Infrastructure.Authorization;

namespace VitalNexus.UnitTests.Authorization;

public sealed class ApplicationRoleAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleRequirementAsync_SucceedsWhenUserHasRequiredRole()
    {
        var userId = Guid.NewGuid();
        var handler = CreateHandler(CreateUser(userId, ApplicationRoles.Clinician));
        var context = CreateContext(new ApplicationRoleRequirement(ApplicationRoles.Clinician));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_SucceedsWhenUserHasOneOfMultipleAllowedRoles()
    {
        var handler = CreateHandler(CreateUser(Guid.NewGuid(), ApplicationRoles.ClinicAdmin));
        var context = CreateContext(new ApplicationRoleRequirement(
            ApplicationRoles.Clinician,
            ApplicationRoles.ClinicAdmin));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_DoesNotSucceedWhenUserHasNoRoles()
    {
        var handler = CreateHandler(CreateUser(Guid.NewGuid()));
        var context = CreateContext(new ApplicationRoleRequirement(ApplicationRoles.Clinician));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_DoesNotSucceedWhenUserIsMissing()
    {
        var handler = CreateHandler(null);
        var context = CreateContext(new ApplicationRoleRequirement(ApplicationRoles.Clinician));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_DoesNotSucceedWhenUserHasDifferentRole()
    {
        var handler = CreateHandler(CreateUser(Guid.NewGuid(), ApplicationRoles.Clinician));
        var context = CreateContext(new ApplicationRoleRequirement(ApplicationRoles.ClinicAdmin));

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleRequirementAsync_MatchesRolesCaseInsensitively()
    {
        var handler = CreateHandler(CreateUser(Guid.NewGuid(), "clinician"));
        var context = CreateContext(new ApplicationRoleRequirement(ApplicationRoles.Clinician));

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static ApplicationRoleAuthorizationHandler CreateHandler(AccountsUser? user) =>
        new(new FakeCurrentAccountsUserAccessor(user));

    private static AuthorizationHandlerContext CreateContext(ApplicationRoleRequirement requirement)
    {
        var user = new System.Security.Claims.ClaimsPrincipal();
        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }

    private static AccountsUser CreateUser(Guid userId, params string[] roles) =>
        new()
        {
            Id = userId,
            EntraObjectId = Guid.NewGuid(),
            Email = "user@example.com",
            Roles = roles,
        };

    private sealed class FakeCurrentAccountsUserAccessor(AccountsUser? user) : ICurrentAccountsUserAccessor
    {
        public Task<AccountsUser?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(user);
    }
}

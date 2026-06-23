using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using VitalNexus.Application.Accounts;

namespace VitalNexus.Infrastructure.Authorization;

public sealed class ApplicationRoleAuthorizationHandler(
    ICurrentAccountsUserAccessor currentAccountsUserAccessor)
    : AuthorizationHandler<ApplicationRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApplicationRoleRequirement requirement)
    {
        var cancellationToken = context.Resource is HttpContext httpContext
            ? httpContext.RequestAborted
            : CancellationToken.None;

        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null || user.Roles.Count == 0)
        {
            return;
        }

        var hasRequiredRole = user.Roles.Any(role =>
            requirement.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));

        if (hasRequiredRole)
        {
            context.Succeed(requirement);
        }
    }
}

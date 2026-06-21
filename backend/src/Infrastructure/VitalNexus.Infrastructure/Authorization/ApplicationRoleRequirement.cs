using Microsoft.AspNetCore.Authorization;

namespace VitalNexus.Infrastructure.Authorization;

public sealed class ApplicationRoleRequirement : IAuthorizationRequirement
{
    public ApplicationRoleRequirement(params string[] allowedRoles)
    {
        ArgumentNullException.ThrowIfNull(allowedRoles);

        if (allowedRoles.Length == 0)
        {
            throw new ArgumentException("At least one application role is required.", nameof(allowedRoles));
        }

        AllowedRoles = allowedRoles;
    }

    public IReadOnlyList<string> AllowedRoles { get; }
}

using System.Security.Claims;

namespace VitalNexus.Infrastructure.Identity;

public static class EntraExternalIdScopeReader
{
    public static IReadOnlyList<string> ReadScopes(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var scopeClaim = user.FindFirst("scp")?.Value;
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            return [];
        }

        return scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static bool HasRequiredScope(ClaimsPrincipal user, string requiredScope)
    {
        if (string.IsNullOrWhiteSpace(requiredScope))
        {
            return false;
        }

        return ReadScopes(user).Contains(requiredScope.Trim(), StringComparer.Ordinal);
    }
}

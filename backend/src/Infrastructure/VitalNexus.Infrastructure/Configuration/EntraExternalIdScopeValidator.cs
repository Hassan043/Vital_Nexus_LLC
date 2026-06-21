using System.Security.Claims;

namespace VitalNexus.Infrastructure.Configuration;

public static class EntraExternalIdScopeValidator
{
    public static bool HasRequiredScope(ClaimsPrincipal user, string requiredScope)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrWhiteSpace(requiredScope))
        {
            return false;
        }

        var scopeClaim = user.FindFirst("scp")?.Value;
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            return false;
        }

        return scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredScope.Trim(), StringComparer.Ordinal);
    }
}

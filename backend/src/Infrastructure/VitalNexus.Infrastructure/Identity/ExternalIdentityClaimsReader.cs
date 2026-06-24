using System.Security.Claims;
using System.Text.Json;
using VitalNexus.Application.Identity;

namespace VitalNexus.Infrastructure.Identity;

public static class ExternalIdentityClaimsReader
{
    public static TrustedExternalIdentity? TryRead(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var objectId = ReadClaim(principal, "oid") ?? ReadClaim(principal, "sub");
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return null;
        }

        return new TrustedExternalIdentity
        {
            ObjectId = objectId,
            Subject = ReadClaim(principal, "sub"),
            TenantId = ReadClaim(principal, "tid"),
            Email = ReadEmail(principal),
            DisplayName = ReadDisplayName(principal),
            Scopes = EntraExternalIdScopeReader.ReadScopes(principal),
        };
    }

    private static string? ReadDisplayName(ClaimsPrincipal principal)
    {
        var name = ReadClaim(principal, "name") ?? principal.Identity?.Name;
        var givenName = ReadClaim(principal, "given_name");
        var familyName = ReadClaim(principal, "family_name");

        if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName))
        {
            var combined = string.Join(' ', new[] { givenName, familyName }.Where(static part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(combined))
            {
                return combined;
            }
        }

        return name;
    }

    private static string? ReadEmail(ClaimsPrincipal principal)
    {
        var preferredUsername = ReadClaim(principal, "preferred_username");
        if (!string.IsNullOrWhiteSpace(preferredUsername))
        {
            return preferredUsername;
        }

        var email = ReadClaim(principal, ClaimTypes.Email) ?? ReadClaim(principal, "email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        return ReadEmailsClaim(principal);
    }

    private static string? ReadEmailsClaim(ClaimsPrincipal principal)
    {
        var emails = ReadClaim(principal, "emails");
        if (string.IsNullOrWhiteSpace(emails))
        {
            return null;
        }

        if (emails.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(emails);
                return parsed?.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
            }
            catch (JsonException)
            {
                return null;
            }
        }

        return emails;
    }

    private static string? ReadClaim(ClaimsPrincipal principal, string claimType)
    {
        var value = principal.FindFirstValue(claimType);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

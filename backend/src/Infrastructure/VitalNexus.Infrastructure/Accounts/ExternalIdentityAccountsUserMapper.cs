using VitalNexus.Application.Accounts;
using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class ExternalIdentityAccountsUserMapper(
    IAccountsUserRepository repository,
    IUserRoleRepository userRoleRepository) : IExternalIdentityAccountsUserMapper
{
    public async Task<AccountsUser> MapAsync(
        TrustedExternalIdentity identity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!Guid.TryParse(identity.ObjectId, out var entraObjectId))
        {
            throw new InvalidOperationException("External identity object id is not a valid GUID.");
        }

        var existingUser = await repository.GetByEntraObjectIdAsync(entraObjectId, cancellationToken);
        if (existingUser is not null)
        {
            return await SyncProfileAsync(existingUser, identity, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(identity.Email))
        {
            throw new InvalidOperationException("Cannot provision an Accounts user without an email address.");
        }

        var newUser = new AccountsUser
        {
            Id = Guid.NewGuid(),
            EntraObjectId = entraObjectId,
            Email = identity.Email.Trim(),
            DisplayName = NormalizeDisplayName(identity.DisplayName),
            CreatedAt = DateTime.UtcNow,
        };

        var createdUser = await repository.CreateAsync(newUser, cancellationToken);
        await userRoleRepository.AssignRoleAsync(createdUser.Id, ApplicationRoles.Clinician, cancellationToken);
        return createdUser;
    }

    private async Task<AccountsUser> SyncProfileAsync(
        AccountsUser existingUser,
        TrustedExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var normalizedDisplayName = NormalizeDisplayName(identity.DisplayName);
        if (string.Equals(existingUser.DisplayName, normalizedDisplayName, StringComparison.Ordinal))
        {
            return existingUser;
        }

        var updatedUser = new AccountsUser
        {
            Id = existingUser.Id,
            EntraObjectId = existingUser.EntraObjectId,
            Email = existingUser.Email,
            DisplayName = normalizedDisplayName,
            CreatedAt = existingUser.CreatedAt,
        };

        return await repository.UpdateAsync(updatedUser, cancellationToken);
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }
}

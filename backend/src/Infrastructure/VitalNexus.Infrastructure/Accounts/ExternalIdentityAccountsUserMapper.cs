using VitalNexus.Application.Accounts;
using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class ExternalIdentityAccountsUserMapper(
    IAccountsUserRepository repository,
    ICustomerRepository customerRepository,
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

        var customerId = Guid.NewGuid();
        var customer = new Customer
        {
            Id = customerId,
            Name = BuildCustomerName(identity.Email),
            CreatedAt = DateTime.UtcNow,
        };

        await customerRepository.CreateAsync(customer, cancellationToken);

        var newUser = new AccountsUser
        {
            Id = Guid.NewGuid(),
            EntraObjectId = entraObjectId,
            CustomerId = customerId,
            Email = identity.Email.Trim(),
            DisplayName = NormalizeDisplayName(identity.DisplayName),
            CreatedAt = DateTime.UtcNow,
        };

        var createdUser = await repository.CreateAsync(newUser, cancellationToken);
        await userRoleRepository.AssignRoleAsync(
            createdUser.Id,
            customerId,
            ApplicationRoles.Admin,
            cancellationToken);

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
            CustomerId = existingUser.CustomerId,
            Email = existingUser.Email,
            DisplayName = normalizedDisplayName,
            CreatedAt = existingUser.CreatedAt,
        };

        return await repository.UpdateAsync(updatedUser, cancellationToken);
    }

    private static string BuildCustomerName(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0
            ? $"Customer ({email[..atIndex]})"
            : "Customer";
    }

    private static string? NormalizeDisplayName(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }
}

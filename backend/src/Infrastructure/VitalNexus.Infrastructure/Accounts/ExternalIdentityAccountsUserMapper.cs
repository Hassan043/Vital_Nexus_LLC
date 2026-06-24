using VitalNexus.Application.Accounts;
using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class ExternalIdentityAccountsUserMapper(
    IAccountsUserRepository repository,
    ICustomerRepository customerRepository,
    IUserRoleRepository userRoleRepository,
    IUserInvitationRepository userInvitationRepository,
    IClinicMembershipRepository clinicMembershipRepository,
    IOnboardingAuditRepository onboardingAuditRepository) : IExternalIdentityAccountsUserMapper
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

        var normalizedEmail = identity.Email.Trim();
        var invitation = await userInvitationRepository.GetPendingByEmailAsync(normalizedEmail, cancellationToken);
        if (invitation is not null)
        {
            return await AcceptInvitationAsync(invitation, entraObjectId, identity, cancellationToken);
        }

        var existingByEmail = await repository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingByEmail is not null && existingByEmail.EntraObjectId is null)
        {
            return await LinkEntraIdentityAsync(existingByEmail, entraObjectId, identity, cancellationToken);
        }

        if (existingByEmail is not null)
        {
            throw new InvalidOperationException("An Accounts user already exists for this email address.");
        }

        return await ProvisionNewCustomerAdminAsync(entraObjectId, identity, normalizedEmail, cancellationToken);
    }

    private async Task<AccountsUser> AcceptInvitationAsync(
        UserInvitation invitation,
        Guid entraObjectId,
        TrustedExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var newUser = new AccountsUser
        {
            Id = Guid.NewGuid(),
            EntraObjectId = entraObjectId,
            CustomerId = invitation.CustomerId,
            Email = invitation.Email.Trim(),
            DisplayName = NormalizeDisplayName(identity.DisplayName),
            AccountStatus = AccountStatuses.Active,
            CreatedAt = DateTime.UtcNow,
        };

        var createdUser = await repository.CreateAsync(newUser, cancellationToken);
        await userRoleRepository.AssignRoleAsync(
            createdUser.Id,
            invitation.CustomerId,
            invitation.RoleName,
            cancellationToken);
        await userInvitationRepository.MarkAcceptedAsync(invitation.Id, cancellationToken);

        var memberships = await clinicMembershipRepository.GetMembershipsForUserAsync(
            invitation.InvitedByUserId,
            cancellationToken);
        foreach (var membership in memberships.Where(m => m.IsActive))
        {
            await clinicMembershipRepository.AddMembershipAsync(
                createdUser.Id,
                membership,
                cancellationToken);
        }

        return createdUser;
    }

    private async Task<AccountsUser> LinkEntraIdentityAsync(
        AccountsUser existingByEmail,
        Guid entraObjectId,
        TrustedExternalIdentity identity,
        CancellationToken cancellationToken)
    {
        var linkedUser = new AccountsUser
        {
            Id = existingByEmail.Id,
            EntraObjectId = entraObjectId,
            CustomerId = existingByEmail.CustomerId,
            Email = existingByEmail.Email,
            DisplayName = NormalizeDisplayName(identity.DisplayName) ?? existingByEmail.DisplayName,
            AccountStatus = AccountStatuses.Active,
            CreatedAt = existingByEmail.CreatedAt,
        };

        return await repository.UpdateAsync(linkedUser, cancellationToken);
    }

    private async Task<AccountsUser> ProvisionNewCustomerAdminAsync(
        Guid entraObjectId,
        TrustedExternalIdentity identity,
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        var customerId = Guid.NewGuid();
        var customerName = BuildCustomerName(normalizedEmail);
        var customer = new Customer
        {
            Id = customerId,
            Name = customerName,
            CreatedAt = DateTime.UtcNow,
        };

        await customerRepository.CreateAsync(customer, cancellationToken);

        var newUser = new AccountsUser
        {
            Id = Guid.NewGuid(),
            EntraObjectId = entraObjectId,
            CustomerId = customerId,
            Email = normalizedEmail,
            DisplayName = NormalizeDisplayName(identity.DisplayName),
            AccountStatus = AccountStatuses.PendingActivation,
            CreatedAt = DateTime.UtcNow,
        };

        var createdUser = await repository.CreateAsync(newUser, cancellationToken);
        await userRoleRepository.AssignRoleAsync(
            createdUser.Id,
            customerId,
            ApplicationRoles.Admin,
            cancellationToken);

        await onboardingAuditRepository.RecordAsync(
            new OnboardingAuditEvent
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ActorUserId = createdUser.Id,
                EventType = OnboardingAuditEventTypes.CustomerCreated,
                Detail = "Initial customer and admin user created via Entra External ID.",
                OccurredAt = DateTime.UtcNow,
            },
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
            AccountStatus = existingUser.AccountStatus,
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

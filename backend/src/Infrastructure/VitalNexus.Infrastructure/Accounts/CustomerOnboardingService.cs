using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Infrastructure.Accounts;

public sealed class CustomerOnboardingService(
    ICustomerRepository customerRepository,
    IAccountsUserRepository accountsUserRepository,
    IUserRoleRepository userRoleRepository,
    ISubscriptionRepository subscriptionRepository,
    IClinicRepository clinicRepository,
    IClinicProfileRepository clinicProfileRepository,
    IClinicMembershipRepository clinicMembershipRepository,
    ICustomerPatientsDatabaseRepository patientsDatabaseRepository,
    IPatientsDatabaseProvisioningService patientsDatabaseProvisioningService,
    IBaaAgreementRepository baaAgreementRepository,
    ICustomerOnboardingStateRepository onboardingStateRepository,
    IOnboardingAuditRepository onboardingAuditRepository,
    IPlanTierRepository planTierRepository) : ICustomerOnboardingService
{
    public async Task<CustomerOnboardingStatus> GetStatusAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
        var users = await accountsUserRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var subscription = await subscriptionRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var clinics = await clinicRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var patientsDatabase = await patientsDatabaseRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var baa = await baaAgreementRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var state = await onboardingStateRepository.GetByCustomerIdAsync(customerId, cancellationToken);

        return await BuildStatusAsync(
            customer,
            users,
            subscription,
            clinics,
            patientsDatabase,
            baa,
            state,
            cancellationToken);
    }

    public async Task<CustomerOnboardingStatus> SignBaaAsync(
        Guid customerId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var existing = await baaAgreementRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (existing is not null)
        {
            return await GetStatusAsync(customerId, cancellationToken);
        }

        await baaAgreementRepository.SignAsync(
            new BaaAgreement
            {
                CustomerId = customerId,
                SignedByUserId = adminUserId,
                SignedAt = DateTime.UtcNow,
            },
            cancellationToken);

        await RecordAuditAsync(customerId, adminUserId, OnboardingAuditEventTypes.BaaSigned, "BAA accepted via demo checkbox.", cancellationToken);
        return await GetStatusAsync(customerId, cancellationToken);
    }

    public async Task<CustomerOnboardingStatus> SelectPlanAsync(
        Guid customerId,
        Guid adminUserId,
        int planTierId,
        int? clientPriceCents,
        CancellationToken cancellationToken = default)
    {
        await EnsureBaaSignedAsync(customerId, cancellationToken);

        if (clientPriceCents.HasValue)
        {
            throw new InvalidOperationException("Client-supplied pricing is not accepted.");
        }

        var planTier = await planTierRepository.GetByIdAsync(planTierId, cancellationToken)
            ?? throw new InvalidOperationException("Plan tier was not found.");

        if (!planTier.IsActive)
        {
            throw new InvalidOperationException("Plan tier is not active.");
        }

        var state = await onboardingStateRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        await onboardingStateRepository.UpsertAsync(
            new CustomerOnboardingState
            {
                CustomerId = customerId,
                SelectedPlanTierId = planTierId,
                ClinicProfileComplete = state?.ClinicProfileComplete ?? false,
                ProvisioningCompletedAt = state?.ProvisioningCompletedAt,
                UpdatedAt = DateTime.UtcNow,
            },
            cancellationToken);

        await RecordAuditAsync(
            customerId,
            adminUserId,
            OnboardingAuditEventTypes.PlanSelected,
            $"Selected plan {planTier.Name}.",
            cancellationToken);

        return await GetStatusAsync(customerId, cancellationToken);
    }

    public async Task<CustomerOnboardingStatus> UpdateClinicProfileAsync(
        Guid customerId,
        Guid adminUserId,
        CompleteOnboardingRequest profileRequest,
        CancellationToken cancellationToken = default)
    {
        await EnsureBaaSignedAsync(customerId, cancellationToken);
        OnboardingValidation.ValidateClinicName(profileRequest.ClinicName);
        OnboardingValidation.ValidateClinicProfile(
            profileRequest.ContactEmail,
            profileRequest.Phone,
            profileRequest.TimeZoneId);

        var clinics = await clinicRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        Clinic clinic;
        if (clinics.Count == 0)
        {
            clinic = new Clinic
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Name = profileRequest.ClinicName.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };
            await clinicRepository.CreateAsync(clinic, cancellationToken);
            await clinicProfileRepository.CreateAsync(
                new ClinicProfile
                {
                    ClinicId = clinic.Id,
                    DisplayName = clinic.Name,
                    ContactEmail = profileRequest.ContactEmail?.Trim(),
                    Phone = profileRequest.Phone?.Trim(),
                    TimeZoneId = profileRequest.TimeZoneId?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                },
                cancellationToken);
            await clinicMembershipRepository.AddMembershipAsync(
                adminUserId,
                new ClinicMembership
                {
                    ClinicId = clinic.Id,
                    ClinicName = clinic.Name,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                },
                cancellationToken);
        }
        else
        {
            clinic = clinics[0];
            var profile = await clinicProfileRepository.GetByClinicIdAsync(clinic.Id, cancellationToken);
            if (profile is null)
            {
                await clinicProfileRepository.CreateAsync(
                    new ClinicProfile
                    {
                        ClinicId = clinic.Id,
                        DisplayName = profileRequest.ClinicName.Trim(),
                        ContactEmail = profileRequest.ContactEmail?.Trim(),
                        Phone = profileRequest.Phone?.Trim(),
                        TimeZoneId = profileRequest.TimeZoneId?.Trim(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }
            else
            {
                await clinicProfileRepository.UpdateAsync(
                    new ClinicProfile
                    {
                        ClinicId = clinic.Id,
                        DisplayName = profileRequest.ClinicName.Trim(),
                        ContactEmail = profileRequest.ContactEmail?.Trim(),
                        Phone = profileRequest.Phone?.Trim(),
                        TimeZoneId = profileRequest.TimeZoneId?.Trim(),
                        CreatedAt = profile.CreatedAt,
                        UpdatedAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }
        }

        var state = await onboardingStateRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        await onboardingStateRepository.UpsertAsync(
            new CustomerOnboardingState
            {
                CustomerId = customerId,
                SelectedPlanTierId = state?.SelectedPlanTierId,
                ClinicProfileComplete = true,
                ProvisioningCompletedAt = state?.ProvisioningCompletedAt,
                UpdatedAt = DateTime.UtcNow,
            },
            cancellationToken);

        await RecordAuditAsync(
            customerId,
            adminUserId,
            OnboardingAuditEventTypes.ClinicProfileUpdated,
            $"Updated clinic profile for {clinic.Name}.",
            cancellationToken);

        return await GetStatusAsync(customerId, cancellationToken);
    }

    public async Task<CustomerOnboardingStatus> CompleteOnboardingAsync(
        Guid customerId,
        Guid adminUserId,
        CompleteOnboardingRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureBaaSignedAsync(customerId, cancellationToken);
        OnboardingValidation.ValidateCustomerDisplayName(request.CustomerDisplayName);
        OnboardingValidation.ValidateClinicName(request.ClinicName);
        OnboardingValidation.ValidateClinicProfile(request.ContactEmail, request.Phone, request.TimeZoneId);

        if (request.PlanTierId <= 0)
        {
            throw new InvalidOperationException("Plan tier is required.");
        }

        await SelectPlanAsync(customerId, adminUserId, request.PlanTierId, clientPriceCents: null, cancellationToken);
        await UpdateClinicProfileAsync(customerId, adminUserId, request, cancellationToken);

        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer was not found.");

        var updatedCustomer = new Customer
        {
            Id = customer.Id,
            Name = request.CustomerDisplayName.Trim(),
            CreatedAt = customer.CreatedAt,
        };
        await customerRepository.UpdateAsync(updatedCustomer, cancellationToken);

        var state = await onboardingStateRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var planTierId = state?.SelectedPlanTierId ?? request.PlanTierId;

        var subscription = await subscriptionRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (subscription is null)
        {
            subscription = await subscriptionRepository.CreateAsync(
                new Subscription
                {
                    CustomerId = customerId,
                    PlanTierId = planTierId,
                    Status = SubscriptionStatuses.Active,
                    CreatedAt = DateTime.UtcNow,
                    ActivatedAt = DateTime.UtcNow,
                },
                cancellationToken);
        }

        await patientsDatabaseProvisioningService.ProvisionForCustomerAsync(customerId, cancellationToken);

        var adminUser = await accountsUserRepository.GetByIdAsync(adminUserId, cancellationToken)
            ?? throw new InvalidOperationException("Admin user was not found.");

        if (string.Equals(adminUser.AccountStatus, AccountStatuses.PendingActivation, StringComparison.Ordinal))
        {
            await accountsUserRepository.UpdateAsync(
                new AccountsUser
                {
                    Id = adminUser.Id,
                    EntraObjectId = adminUser.EntraObjectId,
                    CustomerId = adminUser.CustomerId,
                    Email = adminUser.Email,
                    DisplayName = adminUser.DisplayName,
                    AccountStatus = AccountStatuses.Active,
                    CreatedAt = adminUser.CreatedAt,
                },
                cancellationToken);
        }

        await onboardingStateRepository.UpsertAsync(
            new CustomerOnboardingState
            {
                CustomerId = customerId,
                SelectedPlanTierId = planTierId,
                ClinicProfileComplete = true,
                ProvisioningCompletedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            cancellationToken);

        await RecordAuditAsync(
            customerId,
            adminUserId,
            OnboardingAuditEventTypes.ProvisioningCompleted,
            "Customer onboarding and Patients database provisioning completed.",
            cancellationToken);

        return await GetStatusAsync(customerId, cancellationToken);
    }

    private async Task EnsureBaaSignedAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var baa = await baaAgreementRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (baa is null)
        {
            throw new InvalidOperationException("BAA must be signed before continuing onboarding.");
        }
    }

    private async Task RecordAuditAsync(
        Guid customerId,
        Guid? actorUserId,
        string eventType,
        string? detail,
        CancellationToken cancellationToken)
    {
        await onboardingAuditRepository.RecordAsync(
            new OnboardingAuditEvent
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ActorUserId = actorUserId,
                EventType = eventType,
                Detail = detail,
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);
    }

    private async Task<CustomerOnboardingStatus> BuildStatusAsync(
        Customer? customer,
        IReadOnlyList<AccountsUser> users,
        Subscription? subscription,
        IReadOnlyList<Clinic> clinics,
        CustomerPatientsDatabase? patientsDatabase,
        BaaAgreement? baa,
        CustomerOnboardingState? state,
        CancellationToken cancellationToken)
    {
        var hasAdmin = false;
        foreach (var user in users)
        {
            var roles = await userRoleRepository.GetRoleNamesForUserAsync(user.Id, cancellationToken);
            if (roles.Any(role => string.Equals(role, ApplicationRoles.Admin, StringComparison.OrdinalIgnoreCase)))
            {
                hasAdmin = true;
                break;
            }
        }

        var accountActivated = users.Any(user =>
            string.Equals(user.AccountStatus, AccountStatuses.Active, StringComparison.OrdinalIgnoreCase));

        return new CustomerOnboardingStatus
        {
            CustomerCreated = customer is not null,
            EntraIdentityLinked = users.Any(user => user.EntraObjectId.HasValue),
            BaaSigned = baa is not null,
            PlanSelected = state?.SelectedPlanTierId is not null || subscription is not null,
            ClinicProfileComplete = state?.ClinicProfileComplete == true || clinics.Count > 0,
            SubscriptionCreated = subscription is not null,
            PatientsDatabaseProvisioned = patientsDatabase is { IsActive: true },
            DefaultClinicCreated = clinics.Count > 0,
            AdminAssigned = hasAdmin,
            AccountActivated = accountActivated,
        };
    }
}

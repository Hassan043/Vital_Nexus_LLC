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
    IPatientsDatabaseProvisioningService patientsDatabaseProvisioningService) : ICustomerOnboardingService
{
    private const int DefaultPlanTierId = 1;

    public async Task<CustomerOnboardingStatus> GetStatusAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
        var users = await accountsUserRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var subscription = await subscriptionRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var clinics = await clinicRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var patientsDatabase = await patientsDatabaseRepository.GetByCustomerIdAsync(customerId, cancellationToken);

        return await BuildStatusAsync(customer, users, subscription, clinics, patientsDatabase, cancellationToken);
    }

    public async Task<CustomerOnboardingStatus> CompleteOnboardingAsync(
        Guid customerId,
        Guid adminUserId,
        string customerDisplayName,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer was not found.");

        var subscription = await subscriptionRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (subscription is null)
        {
            subscription = await subscriptionRepository.CreateAsync(
                new Subscription
                {
                    CustomerId = customerId,
                    PlanTierId = DefaultPlanTierId,
                    Status = SubscriptionStatuses.Active,
                    CreatedAt = DateTime.UtcNow,
                    ActivatedAt = DateTime.UtcNow,
                },
                cancellationToken);
        }

        await patientsDatabaseProvisioningService.ProvisionForCustomerAsync(customerId, cancellationToken);

        var clinics = await clinicRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        if (clinics.Count == 0)
        {
            var defaultClinic = await CreateDefaultClinicAsync(customerId, customerDisplayName, cancellationToken);
            await clinicMembershipRepository.AddMembershipAsync(
                adminUserId,
                new ClinicMembership
                {
                    ClinicId = defaultClinic.Id,
                    ClinicName = defaultClinic.Name,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                },
                cancellationToken);
            clinics = [defaultClinic];
        }

        var users = await accountsUserRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var patientsDatabase = await patientsDatabaseRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return await BuildStatusAsync(customer, users, subscription, clinics, patientsDatabase, cancellationToken);
    }

    private async Task<Clinic> CreateDefaultClinicAsync(
        Guid customerId,
        string customerDisplayName,
        CancellationToken cancellationToken)
    {
        var clinicId = Guid.NewGuid();
        var clinic = new Clinic
        {
            Id = clinicId,
            CustomerId = customerId,
            Name = $"{customerDisplayName} — Primary Clinic",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        await clinicRepository.CreateAsync(clinic, cancellationToken);
        await clinicProfileRepository.CreateAsync(
            new ClinicProfile
            {
                ClinicId = clinicId,
                DisplayName = clinic.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            cancellationToken);

        return clinic;
    }

    private async Task<CustomerOnboardingStatus> BuildStatusAsync(
        Customer? customer,
        IReadOnlyList<AccountsUser> users,
        Subscription? subscription,
        IReadOnlyList<Clinic> clinics,
        CustomerPatientsDatabase? patientsDatabase,
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

        return new CustomerOnboardingStatus
        {
            CustomerCreated = customer is not null,
            EntraIdentityLinked = users.Any(user => user.EntraObjectId.HasValue),
            SubscriptionCreated = subscription is not null,
            PatientsDatabaseProvisioned = patientsDatabase is { IsActive: true },
            DefaultClinicCreated = clinics.Count > 0,
            AdminAssigned = hasAdmin,
        };
    }
}

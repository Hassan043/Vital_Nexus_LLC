using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Accounts;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Controllers;

[Authorize(Policy = ApplicationRolePolicies.RequireAdmin)]
[ApiController]
[Route("api/admin")]
public sealed class AdminController(
    ICurrentAccountsUserAccessor currentAccountsUserAccessor,
    ICustomerRepository customerRepository,
    IAccountsUserRepository accountsUserRepository,
    ISubscriptionRepository subscriptionRepository,
    IPlanTierRepository planTierRepository,
    IClinicRepository clinicRepository,
    IClinicProfileRepository clinicProfileRepository,
    ICustomerPatientsDatabaseRepository patientsDatabaseRepository,
    IUserInvitationRepository userInvitationRepository,
    ICustomerOnboardingService customerOnboardingService,
    IOnboardingAuditRepository onboardingAuditRepository) : ControllerBase
{
    [HttpGet("account")]
    public async Task<IActionResult> GetAccountOverview(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var customer = await customerRepository.GetByIdAsync(user.CustomerId, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            customerId = customer.Id,
            customerName = customer.Name,
            userId = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            roles = user.Roles,
            clinicMemberships = user.ClinicMemberships.Select(AccountsUserResponseMapper.MapClinicMembership).ToArray(),
            access = "Admin access to customer account, clinics, staff users, and onboarding information.",
        });
    }

    [HttpGet("onboarding")]
    public async Task<IActionResult> GetOnboardingDashboard(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var customer = await customerRepository.GetByIdAsync(user.CustomerId, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var status = await customerOnboardingService.GetStatusAsync(user.CustomerId, cancellationToken);
        var subscription = await subscriptionRepository.GetByCustomerIdAsync(user.CustomerId, cancellationToken);
        var planTier = subscription is null
            ? null
            : await planTierRepository.GetByIdAsync(subscription.PlanTierId, cancellationToken);
        var clinics = await clinicRepository.GetByCustomerIdAsync(user.CustomerId, cancellationToken);
        var clinicSummaries = new List<object>();
        foreach (var clinic in clinics)
        {
            var profile = await clinicProfileRepository.GetByClinicIdAsync(clinic.Id, cancellationToken);
            clinicSummaries.Add(new
            {
                clinic.Id,
                clinic.Name,
                clinic.IsActive,
                clinic.CreatedAt,
                contactEmail = profile?.ContactEmail,
                phone = profile?.Phone,
                timeZoneId = profile?.TimeZoneId,
            });
        }

        var patientsDatabase = await patientsDatabaseRepository.GetByCustomerIdAsync(user.CustomerId, cancellationToken);
        var staffUsers = await accountsUserRepository.GetByCustomerIdAsync(user.CustomerId, cancellationToken);
        var pendingInvitations = await userInvitationRepository.GetPendingByCustomerIdAsync(
            user.CustomerId,
            cancellationToken);

        return Ok(new
        {
            authenticationProvider = "Microsoft Entra External ID",
            authorizationProvider = "VitalNexus Accounts API",
            customer = new
            {
                customer.Id,
                customer.Name,
                customer.CreatedAt,
            },
            onboarding = status,
            subscription = subscription is null
                ? null
                : new
                {
                    subscription.Status,
                    subscription.CreatedAt,
                    subscription.ActivatedAt,
                    planTier = planTier?.Name,
                    planTierDescription = planTier?.Description,
                    monthlyPriceCents = planTier?.MonthlyPriceCents,
                    patientCapMax = planTier?.PatientCapMax,
                },
            patientsDatabase = patientsDatabase is null
                ? null
                : new
                {
                    patientsDatabase.DatabaseName,
                    patientsDatabase.ServerName,
                    patientsDatabase.ProvisionedAt,
                    patientsDatabase.IsActive,
                    schemaNote = "Contains Placeholder table (demo); clinical tables expand in later phases.",
                },
            clinics = clinicSummaries,
            users = staffUsers.Select(staff => new
            {
                staff.Id,
                staff.Email,
                staff.DisplayName,
                staff.AccountStatus,
                roles = staff.Roles,
                entraLinked = staff.EntraObjectId.HasValue,
            }),
            pendingInvitations = pendingInvitations.Select(invitation => new
            {
                invitation.Id,
                invitation.Email,
                invitation.RoleName,
                invitation.CreatedAt,
            }),
        });
    }

    [HttpPost("users/invite")]
    [HttpPost("staff/invite")]
    public async Task<IActionResult> InviteUser(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required." });
        }

        var normalizedEmail = request.Email.Trim();
        var existingUser = await accountsUserRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existingUser is not null)
        {
            return Conflict(new { error = "A user with this email already exists." });
        }

        var pending = await userInvitationRepository.GetPendingByEmailAsync(normalizedEmail, cancellationToken);
        if (pending is not null)
        {
            return Conflict(new { error = "An invitation is already pending for this email." });
        }

        var roleName = string.IsNullOrWhiteSpace(request.RoleName)
            ? ApplicationRoles.User
            : request.RoleName.Trim();

        if (!string.Equals(roleName, ApplicationRoles.User, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only User role invitations are supported in this demo." });
        }

        var invitation = await userInvitationRepository.CreateAsync(
            new UserInvitation
            {
                Id = Guid.NewGuid(),
                CustomerId = user.CustomerId,
                Email = normalizedEmail,
                RoleName = ApplicationRoles.User,
                InvitedByUserId = user.Id,
                ClinicIds = request.ClinicIds ?? [],
                CreatedAt = DateTime.UtcNow,
            },
            cancellationToken);

        await onboardingAuditRepository.RecordAsync(
            new OnboardingAuditEvent
            {
                Id = Guid.NewGuid(),
                CustomerId = user.CustomerId,
                ActorUserId = user.Id,
                EventType = OnboardingAuditEventTypes.StaffInvited,
                Detail = $"Invited staff user {normalizedEmail}.",
                OccurredAt = DateTime.UtcNow,
            },
            cancellationToken);

        return Created(
            $"/api/admin/onboarding",
            new
            {
                invitation.Id,
                invitation.Email,
                invitation.RoleName,
                message = "Invitation recorded. The staff member must sign in via Microsoft Entra External ID using this email.",
            });
    }

    [HttpPost("clinics")]
    public async Task<IActionResult> CreateClinic(
        [FromBody] CreateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Clinic name is required." });
        }

        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            CustomerId = user.CustomerId,
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        await clinicRepository.CreateAsync(clinic, cancellationToken);

        return Created(
            $"/api/admin/onboarding",
            new
            {
                clinic.Id,
                clinic.Name,
                clinic.CustomerId,
            });
    }

    public sealed class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;

        public string? RoleName { get; set; }

        public IReadOnlyList<Guid>? ClinicIds { get; set; }
    }

    public sealed class CreateClinicRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}

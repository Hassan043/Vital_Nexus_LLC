using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Accounts;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Controllers;

[Authorize(Policy = ApplicationRolePolicies.RequireAdmin)]
[ApiController]
[Route("api/admin/onboarding")]
public sealed class AdminOnboardingController(
    ICurrentAccountsUserAccessor currentAccountsUserAccessor,
    ICustomerOnboardingService customerOnboardingService,
    IOnboardingAuditRepository onboardingAuditRepository,
    IBaaAgreementRepository baaAgreementRepository) : ControllerBase
{
    [HttpPost("baa")]
    public async Task<IActionResult> SignBaa(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var status = await customerOnboardingService.SignBaaAsync(
                user.CustomerId,
                user.Id,
                cancellationToken);

            return Ok(new { onboarding = status, baaSigned = true });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("baa")]
    public async Task<IActionResult> GetBaaStatus(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var agreement = await baaAgreementRepository.GetByCustomerIdAsync(user.CustomerId, cancellationToken);
        return Ok(new
        {
            signed = agreement is not null,
            signedAt = agreement?.SignedAt,
            agreementVersion = agreement?.AgreementVersion ?? "2026.1",
        });
    }

    [HttpPost("plan")]
    public async Task<IActionResult> SelectPlan(
        [FromBody] SelectPlanRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var status = await customerOnboardingService.SelectPlanAsync(
                user.CustomerId,
                user.Id,
                request.PlanTierId,
                request.ClientMonthlyPriceCents,
                cancellationToken);

            return Ok(new { onboarding = status });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("clinic-profile")]
    public async Task<IActionResult> UpdateClinicProfile(
        [FromBody] CompleteOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var status = await customerOnboardingService.UpdateClinicProfileAsync(
                user.CustomerId,
                user.Id,
                request,
                cancellationToken);

            return Ok(new { onboarding = status });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompleteOnboarding(
        [FromBody] CompleteOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        try
        {
            var status = await customerOnboardingService.CompleteOnboardingAsync(
                user.CustomerId,
                user.Id,
                request,
                cancellationToken);

            return Ok(new { onboarding = status });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("audit-events")]
    public async Task<IActionResult> GetAuditEvents(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var events = await onboardingAuditRepository.GetByCustomerIdAsync(user.CustomerId, cancellationToken);
        return Ok(events.Select(entry => new
        {
            entry.Id,
            entry.EventType,
            entry.Detail,
            entry.OccurredAt,
        }));
    }

    public sealed class SelectPlanRequest
    {
        public int PlanTierId { get; set; }

        public int? ClientMonthlyPriceCents { get; set; }
    }
}

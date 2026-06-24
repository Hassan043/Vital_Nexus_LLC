using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/billing")]
public sealed class BillingController(
    IPlanTierRepository planTierRepository,
    IBillingQuoteService billingQuoteService) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await planTierRepository.GetActiveAsync(cancellationToken);
        return Ok(plans.Select(plan => new
        {
            plan.Id,
            plan.Name,
            plan.Description,
            monthlyPriceCents = plan.MonthlyPriceCents,
            patientCapMax = plan.PatientCapMax,
            currency = "USD",
        }));
    }

    [HttpPost("quote")]
    public async Task<IActionResult> CreateQuote(
        [FromBody] BillingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quote = await billingQuoteService.CreateQuoteAsync(
                request.PlanTierId,
                request.ClientMonthlyPriceCents,
                cancellationToken);

            return Ok(quote);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    public sealed class BillingQuoteRequest
    {
        public int PlanTierId { get; set; }

        public int? ClientMonthlyPriceCents { get; set; }
    }
}

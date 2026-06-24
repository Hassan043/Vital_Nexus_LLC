using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Accounts;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Controllers;

[Authorize(Policy = ApplicationRolePolicies.RequireAdmin)]
[ApiController]
[Route("api/admin/plan-tiers")]
public sealed class AdminPlanTiersController(IPlanTierRepository planTierRepository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlanTiers(CancellationToken cancellationToken)
    {
        var plans = await planTierRepository.GetActiveAsync(cancellationToken);
        return Ok(plans);
    }

    [HttpPut("{planTierId:int}")]
    public async Task<IActionResult> UpdatePlanTier(
        int planTierId,
        [FromBody] UpdatePlanTierRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await planTierRepository.GetByIdAsync(planTierId, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (request.ClientMonthlyPriceCents.HasValue)
        {
            return BadRequest(new { error = "Client-supplied pricing is not accepted." });
        }

        var updated = await planTierRepository.UpsertAsync(
            new PlanTier
            {
                Id = planTierId,
                Name = string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name.Trim(),
                Description = request.Description ?? existing.Description,
                MonthlyPriceCents = request.MonthlyPriceCents ?? existing.MonthlyPriceCents,
                PatientCapMax = request.PatientCapMax ?? existing.PatientCapMax,
                IsActive = request.IsActive ?? existing.IsActive,
            },
            cancellationToken);

        return Ok(updated);
    }

    public sealed class UpdatePlanTierRequest
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        public int? MonthlyPriceCents { get; set; }

        public int? ClientMonthlyPriceCents { get; set; }

        public int? PatientCapMax { get; set; }

        public bool? IsActive { get; set; }
    }
}

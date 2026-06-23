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
    ICustomerRepository customerRepository) : ControllerBase
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
}

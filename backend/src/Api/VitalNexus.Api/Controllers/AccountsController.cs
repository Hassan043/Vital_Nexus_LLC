using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Accounts;
using VitalNexus.Application.Accounts;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Controllers;

[Authorize(Policy = ApplicationRolePolicies.RequireCustomerMember)]
[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(ICurrentAccountsUserAccessor currentAccountsUserAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrentAccount(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            customerId = user.CustomerId,
            userId = user.Id,
            entraObjectId = user.EntraObjectId,
            email = user.Email,
            displayName = user.DisplayName,
            createdAt = user.CreatedAt,
            roles = user.Roles,
            clinicMemberships = user.ClinicMemberships.Select(AccountsUserResponseMapper.MapClinicMembership).ToArray(),
        });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Accounts;
using VitalNexus.Application.Accounts;
using VitalNexus.Application.Identity;
using VitalNexus.Domain.Accounts;

namespace VitalNexus.Api.Controllers;

[Authorize(Policy = ApplicationRolePolicies.RequireProviderRole)]
[ApiController]
[Route("api/me")]
public sealed class MeController(
    IExternalIdentityAccessor externalIdentityAccessor,
    ICurrentAccountsUserAccessor currentAccountsUserAccessor,
    ICurrentClinicContextAccessor currentClinicContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var identity = externalIdentityAccessor.Current;
        if (identity is null)
        {
            return Unauthorized();
        }

        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var activeClinic = await currentClinicContextAccessor.GetCurrentAsync(cancellationToken);

        return Ok(new
        {
            userId = user.Id,
            objectId = user.EntraObjectId,
            entraObjectId = user.EntraObjectId,
            name = user.DisplayName,
            email = user.Email,
            tenantId = identity.TenantId,
            scopes = identity.Scopes.Count == 0 ? null : string.Join(' ', identity.Scopes),
            roles = user.Roles,
            clinicMemberships = user.ClinicMemberships.Select(AccountsUserResponseMapper.MapClinicMembership).ToArray(),
            activeClinic = AccountsUserResponseMapper.MapActiveClinic(activeClinic),
        });
    }
}

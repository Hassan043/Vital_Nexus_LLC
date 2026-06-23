using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Application.Identity;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = EntraExternalIdAuthenticationExtensions.ApiAccessPolicyName)]
public sealed class MeController(IExternalIdentityAccessor externalIdentityAccessor) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var identity = externalIdentityAccessor.Current;
        if (identity is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            objectId = identity.ObjectId,
            name = identity.DisplayName,
            email = identity.Email,
            tenantId = identity.TenantId,
            scopes = identity.Scopes.Count == 0 ? null : string.Join(' ', identity.Scopes),
        });
    }
}

using Microsoft.AspNetCore.Mvc;
using VitalNexus.Application.Identity;

namespace VitalNexus.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(IExternalIdentityAccessor externalIdentityAccessor) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCurrentAccount()
    {
        var identity = externalIdentityAccessor.Current;
        if (identity is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            objectId = identity.ObjectId,
            email = identity.Email,
            displayName = identity.DisplayName,
            tenantId = identity.TenantId,
        });
    }
}

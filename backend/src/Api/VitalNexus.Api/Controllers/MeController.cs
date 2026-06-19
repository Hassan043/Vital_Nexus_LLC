using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.Infrastructure.Configuration;

namespace VitalNexus.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize(Policy = EntraExternalIdAuthenticationExtensions.ApiAccessPolicyName)]
public sealed class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            objectId = User.FindFirstValue("oid") ?? User.FindFirstValue("sub"),
            name = User.FindFirstValue("name") ?? User.Identity?.Name,
            email = User.FindFirstValue("preferred_username")
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("emails"),
            scopes = User.FindFirstValue("scp"),
        });
    }
}

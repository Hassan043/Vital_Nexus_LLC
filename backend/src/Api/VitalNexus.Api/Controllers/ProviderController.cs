using Microsoft.AspNetCore.Mvc;
using VitalNexus.Application.Identity;

namespace VitalNexus.Api.Controllers;

[ApiController]
[Route("api/provider")]
public sealed class ProviderController(IExternalIdentityAccessor externalIdentityAccessor) : ControllerBase
{
    [HttpGet]
    public IActionResult GetCurrentProvider()
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
            onboardingStatus = "pending",
        });
    }
}

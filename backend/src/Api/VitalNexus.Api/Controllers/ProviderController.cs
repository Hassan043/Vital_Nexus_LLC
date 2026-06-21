using Microsoft.AspNetCore.Mvc;
using VitalNexus.Api.Accounts;
using VitalNexus.Application.Accounts;

namespace VitalNexus.Api.Controllers;

[ApiController]
[Route("api/provider")]
public sealed class ProviderController(
    ICurrentAccountsUserAccessor currentAccountsUserAccessor,
    ICurrentClinicContextAccessor currentClinicContextAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrentProvider(CancellationToken cancellationToken)
    {
        var user = await currentAccountsUserAccessor.GetCurrentAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var activeClinic = await currentClinicContextAccessor.GetCurrentAsync(cancellationToken);

        return Ok(new
        {
            userId = user.Id,
            entraObjectId = user.EntraObjectId,
            email = user.Email,
            displayName = user.DisplayName,
            roles = user.Roles,
            clinicMemberships = user.ClinicMemberships.Select(AccountsUserResponseMapper.MapClinicMembership).ToArray(),
            activeClinic = AccountsUserResponseMapper.MapActiveClinic(activeClinic),
            onboardingStatus = ProviderOnboardingStatus.FromMemberships(user.ClinicMemberships),
        });
    }
}

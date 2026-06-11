using Microsoft.AspNetCore.Mvc;

namespace VitalNexus.Api.Controllers;

/// <summary>
/// Liveness/readiness endpoint. Returns no PHI — safe for uptime checks and smoke tests.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "VitalNexus.Api",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        utc = DateTime.UtcNow.ToString("O"),
    });
}

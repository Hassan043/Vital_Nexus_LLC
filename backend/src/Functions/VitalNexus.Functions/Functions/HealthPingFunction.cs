using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace VitalNexus.Functions.Functions;

/// <summary>
/// Baseline timer function proving the isolated worker host runs and emits telemetry.
/// Runs every 5 minutes. Logs only non-PHI operational data.
/// Replaced/joined by real jobs (AI analysis, retention scans, exports) in later phases.
/// </summary>
public sealed class HealthPingFunction
{
    private readonly ILogger<HealthPingFunction> _logger;

    public HealthPingFunction(ILogger<HealthPingFunction> logger) => _logger = logger;

    [Function(nameof(HealthPingFunction))]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
    {
        _logger.LogInformation(
            "VitalNexus Functions host heartbeat at {UtcNow:O}. Next run: {Next}",
            DateTime.UtcNow,
            timer.ScheduleStatus?.Next);
    }
}

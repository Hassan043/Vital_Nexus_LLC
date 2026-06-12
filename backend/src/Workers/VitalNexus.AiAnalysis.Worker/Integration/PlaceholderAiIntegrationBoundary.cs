using VitalNexus.AiAnalysis.Worker.Logging;
using VitalNexus.AiAnalysis.Worker.Models;

namespace VitalNexus.AiAnalysis.Worker.Integration;

/// <summary>
/// Placeholder AI integration — defers to Phase 4 for production provider wiring.
/// </summary>
public sealed class PlaceholderAiIntegrationBoundary : IAiIntegrationBoundary
{
    private readonly ILogger<PlaceholderAiIntegrationBoundary> _logger;

    public PlaceholderAiIntegrationBoundary(ILogger<PlaceholderAiIntegrationBoundary> logger)
    {
        _logger = logger;
    }

    public Task<AiIntegrationOutcome> AnalyzeAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        SanitizedWorkerLogs.LogIntegrationCall(_logger, request.RequestId, "placeholder-analyze");

        return Task.FromResult(new AiIntegrationOutcome(
            AiIntegrationOutcomeStatus.Skipped,
            SummaryToken: "placeholder"));
    }
}

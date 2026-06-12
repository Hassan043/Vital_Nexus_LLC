using VitalNexus.AiAnalysis.Worker.Logging;
using VitalNexus.AiAnalysis.Worker.Models;

namespace VitalNexus.AiAnalysis.Worker.Persistence;

/// <summary>
/// Placeholder persistence — real storage wiring arrives with FunctionOperations DB.
/// </summary>
public sealed class PlaceholderAiAnalysisResultStore : IAiAnalysisResultStore
{
    private readonly ILogger<PlaceholderAiAnalysisResultStore> _logger;

    public PlaceholderAiAnalysisResultStore(ILogger<PlaceholderAiAnalysisResultStore> logger)
    {
        _logger = logger;
    }

    public Task PersistAsync(AiAnalysisResult result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AI analysis result persisted (placeholder). RequestId={RequestId}, Status={Status}, MarkersEvaluated={MarkersEvaluated}",
            result.RequestId,
            result.Status,
            result.MarkersEvaluated);

        return Task.CompletedTask;
    }
}

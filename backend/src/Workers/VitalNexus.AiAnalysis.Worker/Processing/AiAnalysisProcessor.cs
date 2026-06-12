using VitalNexus.AiAnalysis.Worker.Integration;
using VitalNexus.AiAnalysis.Worker.Logging;
using VitalNexus.AiAnalysis.Worker.Models;
using VitalNexus.AiAnalysis.Worker.Persistence;

namespace VitalNexus.AiAnalysis.Worker.Processing;

public interface IAiAnalysisProcessor
{
    Task<AiAnalysisResult> ProcessAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Validates anonymized requests, invokes the approved AI integration boundary,
/// and persists results. Idempotent and safe to retry.
/// </summary>
public sealed class AiAnalysisProcessor : IAiAnalysisProcessor
{
    private readonly IAiIntegrationBoundary _integrationBoundary;
    private readonly IAiAnalysisResultStore _resultStore;
    private readonly ILogger<AiAnalysisProcessor> _logger;

    public AiAnalysisProcessor(
        IAiIntegrationBoundary integrationBoundary,
        IAiAnalysisResultStore resultStore,
        ILogger<AiAnalysisProcessor> logger)
    {
        _integrationBoundary = integrationBoundary;
        _resultStore = resultStore;
        _logger = logger;
    }

    public async Task<AiAnalysisResult> ProcessAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validationResult = Validate(request);
        if (validationResult is not null)
        {
            SanitizedWorkerLogs.LogProcessingRejected(_logger, request.RequestId, validationResult.RejectionReason!);
            await _resultStore.PersistAsync(validationResult, cancellationToken).ConfigureAwait(false);
            return validationResult;
        }

        var flagged = request.Markers
            .Where(m => m.Value < m.ReferenceLow || m.Value > m.ReferenceHigh)
            .Select(m => m.Name)
            .ToArray();

        await _integrationBoundary.AnalyzeAsync(request, cancellationToken).ConfigureAwait(false);

        var result = new AiAnalysisResult(
            request.RequestId,
            AiAnalysisStatus.Completed,
            request.Markers.Count,
            flagged);

        SanitizedWorkerLogs.LogProcessingCompleted(
            _logger,
            request.RequestId,
            request.AnonymousPatientId,
            result.MarkersEvaluated,
            flagged.Length);

        await _resultStore.PersistAsync(result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static AiAnalysisResult? Validate(AiAnalysisRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Rejected(request.RequestId, "Missing request identifier.");
        }

        if (string.IsNullOrWhiteSpace(request.AnonymousPatientId))
        {
            return Rejected(request.RequestId, "Missing anonymous patient identifier.");
        }

        if (request.Markers is not { Count: > 0 })
        {
            return Rejected(request.RequestId, "No markers to evaluate.");
        }

        if (request.Markers.Any(m => string.IsNullOrWhiteSpace(m.Name) || m.ReferenceLow > m.ReferenceHigh))
        {
            return Rejected(request.RequestId, "One or more markers are malformed.");
        }

        return null;
    }

    private static AiAnalysisResult Rejected(string requestId, string reason) =>
        new(
            requestId,
            AiAnalysisStatus.Rejected,
            MarkersEvaluated: 0,
            FlaggedMarkers: Array.Empty<string>(),
            RejectionReason: reason);
}

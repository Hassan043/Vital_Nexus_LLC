using VitalNexus.AiAnalysis.Worker.Logging;
using VitalNexus.AiAnalysis.Worker.Models;
using VitalNexus.AiAnalysis.Worker.Processing;
using VitalNexus.AiAnalysis.Worker.Retry;

namespace VitalNexus.AiAnalysis.Worker.Handlers;

public interface IAiAnalysisQueueHandler
{
    Task<AiAnalysisQueueHandleResult> HandleAsync(
        AiAnalysisQueueMessage message,
        CancellationToken cancellationToken = default);
}

public enum AiAnalysisQueueHandleStatus
{
    Completed,
    Rejected,
    Failed
}

public sealed record AiAnalysisQueueHandleResult(
    AiAnalysisQueueHandleStatus Status,
    string RequestId,
    string? Detail = null);

/// <summary>
/// Entry point for Dapr-delivered AI analysis queue messages.
/// </summary>
public sealed class AiAnalysisQueueHandler : IAiAnalysisQueueHandler
{
    private readonly IAiAnalysisProcessor _processor;
    private readonly IWorkerRetryExecutor _retryExecutor;
    private readonly ILogger<AiAnalysisQueueHandler> _logger;

    public AiAnalysisQueueHandler(
        IAiAnalysisProcessor processor,
        IWorkerRetryExecutor retryExecutor,
        ILogger<AiAnalysisQueueHandler> logger)
    {
        _processor = processor;
        _retryExecutor = retryExecutor;
        _logger = logger;
    }

    public async Task<AiAnalysisQueueHandleResult> HandleAsync(
        AiAnalysisQueueMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SanitizedWorkerLogs.LogQueueMessageReceived(
            _logger,
            message.RequestId,
            message.AnonymousPatientId,
            message.Markers.Count);

        var request = new AiAnalysisRequest(
            message.RequestId,
            message.AnonymousPatientId,
            message.Markers);

        try
        {
            AiAnalysisResult? result = null;

            await _retryExecutor.ExecuteAsync(
                message.RequestId,
                async ct => result = await _processor.ProcessAsync(request, ct).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            return result!.Status switch
            {
                AiAnalysisStatus.Completed => new AiAnalysisQueueHandleResult(
                    AiAnalysisQueueHandleStatus.Completed,
                    result.RequestId),
                AiAnalysisStatus.Rejected => new AiAnalysisQueueHandleResult(
                    AiAnalysisQueueHandleStatus.Rejected,
                    result.RequestId,
                    result.RejectionReason),
                _ => new AiAnalysisQueueHandleResult(
                    AiAnalysisQueueHandleStatus.Failed,
                    result.RequestId,
                    "Unexpected processing status.")
            };
        }
        catch (WorkerRetryExhaustedException ex)
        {
            return new AiAnalysisQueueHandleResult(
                AiAnalysisQueueHandleStatus.Failed,
                message.RequestId,
                ex.Message);
        }
    }
}

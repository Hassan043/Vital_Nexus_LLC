namespace VitalNexus.AiAnalysis.Worker.Logging;

/// <summary>
/// Helpers that enforce PHI-safe logging: only operation identifiers, counts,
/// and status values — never marker names, lab values, AI prompts/responses,
/// or clinical notes.
/// </summary>
public static class SanitizedWorkerLogs
{
    public static void LogQueueMessageReceived(
        ILogger logger,
        string requestId,
        string anonymousPatientId,
        int markerCount)
    {
        logger.LogInformation(
            "AI analysis queue message received. RequestId={RequestId}, AnonymousPatientId={AnonymousPatientId}, MarkerCount={MarkerCount}",
            requestId,
            anonymousPatientId,
            markerCount);
    }

    public static void LogProcessingCompleted(
        ILogger logger,
        string requestId,
        string anonymousPatientId,
        int markersEvaluated,
        int flaggedCount)
    {
        logger.LogInformation(
            "AI analysis processing completed. RequestId={RequestId}, AnonymousPatientId={AnonymousPatientId}, MarkersEvaluated={MarkersEvaluated}, FlaggedCount={FlaggedCount}",
            requestId,
            anonymousPatientId,
            markersEvaluated,
            flaggedCount);
    }

    public static void LogProcessingRejected(
        ILogger logger,
        string requestId,
        string reason)
    {
        logger.LogWarning(
            "AI analysis processing rejected. RequestId={RequestId}, Reason={Reason}",
            requestId,
            reason);
    }

    public static void LogRetryScheduled(
        ILogger logger,
        string requestId,
        int attempt,
        int maxAttempts,
        int delaySeconds,
        string errorCategory)
    {
        logger.LogWarning(
            "AI analysis retry scheduled. RequestId={RequestId}, Attempt={Attempt}, MaxAttempts={MaxAttempts}, DelaySeconds={DelaySeconds}, ErrorCategory={ErrorCategory}",
            requestId,
            attempt,
            maxAttempts,
            delaySeconds,
            errorCategory);
    }

    public static void LogRetryExhausted(
        ILogger logger,
        string requestId,
        int maxAttempts,
        string errorCategory)
    {
        logger.LogError(
            "AI analysis retries exhausted. RequestId={RequestId}, MaxAttempts={MaxAttempts}, ErrorCategory={ErrorCategory}",
            requestId,
            maxAttempts,
            errorCategory);
    }

    public static void LogIntegrationCall(
        ILogger logger,
        string requestId,
        string integrationPhase)
    {
        logger.LogInformation(
            "AI integration boundary invoked. RequestId={RequestId}, Phase={Phase}",
            requestId,
            integrationPhase);
    }
}

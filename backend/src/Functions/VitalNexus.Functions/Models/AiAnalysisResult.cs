namespace VitalNexus.Functions.Models;

/// <summary>Outcome of processing an <see cref="AiAnalysisRequest"/>.</summary>
public enum AiAnalysisStatus
{
    Completed,
    Rejected
}

/// <summary>
/// Result of an AI analysis pass over an anonymized request.
/// Flagged marker names flow to downstream processing and storage — they are
/// never written to logs or telemetry.
/// </summary>
public sealed record AiAnalysisResult(
    string RequestId,
    AiAnalysisStatus Status,
    int MarkersEvaluated,
    IReadOnlyList<string> FlaggedMarkers,
    string? RejectionReason = null);

namespace VitalNexus.AiAnalysis.Worker.Models;

/// <summary>
/// A single anonymized lab marker submitted for AI analysis.
/// Carries only the marker name, measured value, unit, and reference range —
/// never any patient-identifying information.
/// </summary>
public sealed record AiAnalysisMarker(
    string Name,
    decimal Value,
    string Unit,
    decimal ReferenceLow,
    decimal ReferenceHigh);

/// <summary>
/// An anonymized AI analysis request. No PHI (names, DOB, contact details,
/// MRN, clinical notes) may ever appear on this contract.
/// </summary>
public sealed record AiAnalysisRequest(
    string RequestId,
    string AnonymousPatientId,
    IReadOnlyList<AiAnalysisMarker> Markers);

/// <summary>Outcome of processing an <see cref="AiAnalysisRequest"/>.</summary>
public enum AiAnalysisStatus
{
    Completed,
    Rejected
}

/// <summary>
/// Result of an AI analysis pass. Flagged marker names are persisted downstream —
/// they are never written to logs or telemetry.
/// </summary>
public sealed record AiAnalysisResult(
    string RequestId,
    AiAnalysisStatus Status,
    int MarkersEvaluated,
    IReadOnlyList<string> FlaggedMarkers,
    string? RejectionReason = null);

/// <summary>
/// Dapr pub/sub envelope for queued AI analysis work.
/// </summary>
public sealed record AiAnalysisQueueMessage(
    string RequestId,
    string AnonymousPatientId,
    IReadOnlyList<AiAnalysisMarker> Markers);

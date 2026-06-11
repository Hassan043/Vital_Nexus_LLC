namespace VitalNexus.Functions.Models;

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
/// An anonymized AI analysis request. This is the only shape the AI analysis
/// pipeline accepts: an operation identifier, the anonymous patient identifier,
/// and anonymized lab values with reference ranges. No PHI (names, DOB, contact
/// details, MRN, clinical notes) may ever appear on this contract.
/// </summary>
public sealed record AiAnalysisRequest(
    string RequestId,
    string AnonymousPatientId,
    IReadOnlyList<AiAnalysisMarker> Markers);

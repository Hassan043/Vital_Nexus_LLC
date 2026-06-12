using VitalNexus.AiAnalysis.Worker.Models;

namespace VitalNexus.AiAnalysis.Worker.Integration;

/// <summary>
/// Approved boundary for calling external AI providers. Implementations must
/// never log prompts, responses, or raw lab values.
/// </summary>
public interface IAiIntegrationBoundary
{
    Task<AiIntegrationOutcome> AnalyzeAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

public enum AiIntegrationOutcomeStatus
{
    Succeeded,
    Skipped
}

/// <summary>
/// Placeholder outcome — production Claude/OpenAI logic is out of scope for F1.T2.3.
/// </summary>
public sealed record AiIntegrationOutcome(
    AiIntegrationOutcomeStatus Status,
    string SummaryToken);

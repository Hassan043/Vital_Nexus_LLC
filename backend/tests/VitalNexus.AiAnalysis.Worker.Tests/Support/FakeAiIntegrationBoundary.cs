using VitalNexus.AiAnalysis.Worker.Integration;
using VitalNexus.AiAnalysis.Worker.Models;

namespace VitalNexus.AiAnalysis.Worker.Tests.Support;

/// <summary>Mock AI integration boundary — no live provider calls or credentials.</summary>
internal sealed class FakeAiIntegrationBoundary : IAiIntegrationBoundary
{
    public int AnalyzeCallCount { get; private set; }

    public Task<AiIntegrationOutcome> AnalyzeAsync(
        AiAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        AnalyzeCallCount++;
        return Task.FromResult(new AiIntegrationOutcome(
            AiIntegrationOutcomeStatus.Skipped,
            SummaryToken: "mock"));
    }
}

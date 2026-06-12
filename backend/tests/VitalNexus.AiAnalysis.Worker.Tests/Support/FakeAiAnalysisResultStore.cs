using VitalNexus.AiAnalysis.Worker.Models;
using VitalNexus.AiAnalysis.Worker.Persistence;

namespace VitalNexus.AiAnalysis.Worker.Tests.Support;

internal sealed class FakeAiAnalysisResultStore : IAiAnalysisResultStore
{
    public List<AiAnalysisResult> PersistedResults { get; } = new();

    public Task PersistAsync(AiAnalysisResult result, CancellationToken cancellationToken = default)
    {
        PersistedResults.Add(result);
        return Task.CompletedTask;
    }
}

using VitalNexus.AiAnalysis.Worker.Models;

namespace VitalNexus.AiAnalysis.Worker.Persistence;

public interface IAiAnalysisResultStore
{
    Task PersistAsync(AiAnalysisResult result, CancellationToken cancellationToken = default);
}

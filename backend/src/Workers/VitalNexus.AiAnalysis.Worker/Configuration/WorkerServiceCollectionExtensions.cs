using VitalNexus.AiAnalysis.Worker.Handlers;
using VitalNexus.AiAnalysis.Worker.Integration;
using VitalNexus.AiAnalysis.Worker.Persistence;
using VitalNexus.AiAnalysis.Worker.Processing;
using VitalNexus.AiAnalysis.Worker.Retry;

namespace VitalNexus.AiAnalysis.Worker.Configuration;

public static class WorkerServiceCollectionExtensions
{
    public static IServiceCollection AddAiAnalysisWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));
        services.Configure<DaprPubSubOptions>(configuration.GetSection(DaprPubSubOptions.SectionName));
        services.Configure<WorkerRetryOptions>(configuration.GetSection(WorkerRetryOptions.SectionName));

        services.AddSingleton<IWorkerRetryExecutor, WorkerRetryExecutor>();
        services.AddScoped<IAiAnalysisProcessor, AiAnalysisProcessor>();
        services.AddScoped<IAiAnalysisQueueHandler, AiAnalysisQueueHandler>();
        services.AddScoped<IAiIntegrationBoundary, PlaceholderAiIntegrationBoundary>();
        services.AddScoped<IAiAnalysisResultStore, PlaceholderAiAnalysisResultStore>();

        return services;
    }
}

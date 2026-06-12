namespace VitalNexus.AiAnalysis.Worker.Configuration;

/// <summary>
/// Dapr pub/sub component and topic names. Values must match the Dapr component
/// manifests deployed with the worker container (see infra in a later issue).
/// </summary>
public sealed class DaprPubSubOptions
{
    public const string SectionName = "Dapr";

    /// <summary>Name of the Dapr pub/sub component (e.g. redis-pubsub, servicebus-pubsub).</summary>
    public string PubSubComponentName { get; set; } = "pubsub";

    /// <summary>Topic for queued AI analysis work items.</summary>
    public string AiAnalysisTopicName { get; set; } = "ai-analysis-queue";
}

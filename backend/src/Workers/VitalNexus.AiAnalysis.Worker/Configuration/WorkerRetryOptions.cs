namespace VitalNexus.AiAnalysis.Worker.Configuration;

public sealed class WorkerRetryOptions
{
    public const string SectionName = "Retry";

    public int MaxAttempts { get; set; } = 3;

    public int InitialDelaySeconds { get; set; } = 2;

    public int MaxDelaySeconds { get; set; } = 30;
}

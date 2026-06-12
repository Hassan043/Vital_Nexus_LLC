namespace VitalNexus.AiAnalysis.Worker.Configuration;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string ServiceName { get; set; } = "VitalNexus.AiAnalysis.Worker";
}

namespace VitalNexus.AiAnalysis.Worker.Retry;

public enum WorkerErrorCategory
{
    Transient,
    Permanent
}

public sealed class WorkerRetryExhaustedException : Exception
{
    public WorkerRetryExhaustedException(string requestId, int maxAttempts, Exception innerException)
        : base($"Retry attempts exhausted for request '{requestId}' after {maxAttempts} attempts.", innerException)
    {
        RequestId = requestId;
        MaxAttempts = maxAttempts;
    }

    public string RequestId { get; }

    public int MaxAttempts { get; }
}

public interface IWorkerRetryExecutor
{
    Task ExecuteAsync(
        string requestId,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}

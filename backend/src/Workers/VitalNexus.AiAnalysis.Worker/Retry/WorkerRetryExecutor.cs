using Microsoft.Extensions.Options;
using VitalNexus.AiAnalysis.Worker.Configuration;
using VitalNexus.AiAnalysis.Worker.Logging;

namespace VitalNexus.AiAnalysis.Worker.Retry;

public sealed class WorkerRetryExecutor : IWorkerRetryExecutor
{
    private readonly WorkerRetryOptions _options;
    private readonly ILogger<WorkerRetryExecutor> _logger;

    public WorkerRetryExecutor(
        IOptions<WorkerRetryOptions> options,
        ILogger<WorkerRetryExecutor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        string requestId,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        var maxAttempts = Math.Max(1, _options.MaxAttempts);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await operation(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);

                SanitizedWorkerLogs.LogRetryScheduled(
                    _logger,
                    requestId,
                    attempt,
                    maxAttempts,
                    delay,
                    WorkerErrorCategory.Transient.ToString());

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        SanitizedWorkerLogs.LogRetryExhausted(
            _logger,
            requestId,
            maxAttempts,
            Classify(lastException).ToString());

        throw new WorkerRetryExhaustedException(
            requestId,
            maxAttempts,
            lastException ?? new InvalidOperationException("Retry failed without a captured exception."));
    }

    private int CalculateDelay(int attempt)
    {
        var exponential = _options.InitialDelaySeconds * Math.Pow(2, attempt - 1);
        return (int)Math.Min(exponential, _options.MaxDelaySeconds);
    }

    private static bool IsTransient(Exception exception) =>
        exception is HttpRequestException or TimeoutException;

    private static WorkerErrorCategory Classify(Exception? exception) =>
        exception is not null && IsTransient(exception)
            ? WorkerErrorCategory.Transient
            : WorkerErrorCategory.Permanent;
}

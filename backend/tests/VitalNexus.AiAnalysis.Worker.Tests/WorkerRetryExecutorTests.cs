using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VitalNexus.AiAnalysis.Worker.Configuration;
using VitalNexus.AiAnalysis.Worker.Retry;
using VitalNexus.AiAnalysis.Worker.Tests.Support;

namespace VitalNexus.AiAnalysis.Worker.Tests;

public class WorkerRetryExecutorTests
{
    private readonly CapturingLogger<WorkerRetryExecutor> _logger = new();

    [Fact]
    public async Task ExecuteAsync_RetriesTransientFailuresThenSucceeds()
    {
        var executor = CreateExecutor(maxAttempts: 3);
        var attempts = 0;

        await executor.ExecuteAsync(
            WorkerTestData.FakeRequestId,
            _ =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw new HttpRequestException("Transient failure.");
                }

                return Task.CompletedTask;
            });

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenRetriesAreExhausted()
    {
        var executor = CreateExecutor(maxAttempts: 2);

        var exception = await Assert.ThrowsAsync<WorkerRetryExhaustedException>(() =>
            executor.ExecuteAsync(
                WorkerTestData.FakeRequestId,
                _ => throw new HttpRequestException("Persistent transient failure.")));

        Assert.Equal(WorkerTestData.FakeRequestId, exception.RequestId);
        Assert.Equal(2, exception.MaxAttempts);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryPermanentFailures()
    {
        var executor = CreateExecutor(maxAttempts: 3);
        var attempts = 0;

        await Assert.ThrowsAsync<WorkerRetryExhaustedException>(() =>
            executor.ExecuteAsync(
                WorkerTestData.FakeRequestId,
                _ =>
                {
                    attempts++;
                    throw new InvalidOperationException("Permanent failure.");
                }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_LogsRetryMetadataWithoutSensitiveValues()
    {
        var executor = CreateExecutor(maxAttempts: 2);

        await Assert.ThrowsAsync<WorkerRetryExhaustedException>(() =>
            executor.ExecuteAsync(
                WorkerTestData.FakeRequestId,
                _ => throw new HttpRequestException("Transient failure.")));

        Assert.Contains(_logger.Messages, message => message.Contains("retry scheduled", StringComparison.OrdinalIgnoreCase));
        Assert.All(_logger.Messages, message =>
        {
            Assert.DoesNotContain(WorkerTestData.FakeSensitiveMarkerName, message);
            Assert.DoesNotContain(WorkerTestData.FakeSensitiveMarkerValue.ToString(), message);
        });
    }

    private WorkerRetryExecutor CreateExecutor(int maxAttempts) =>
        new(
            Options.Create(new WorkerRetryOptions
            {
                MaxAttempts = maxAttempts,
                InitialDelaySeconds = 0,
                MaxDelaySeconds = 0,
            }),
            _logger);
}

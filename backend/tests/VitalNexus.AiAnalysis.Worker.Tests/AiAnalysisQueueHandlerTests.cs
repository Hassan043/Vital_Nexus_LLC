using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VitalNexus.AiAnalysis.Worker.Configuration;
using VitalNexus.AiAnalysis.Worker.Handlers;
using VitalNexus.AiAnalysis.Worker.Integration;
using VitalNexus.AiAnalysis.Worker.Models;
using VitalNexus.AiAnalysis.Worker.Processing;
using VitalNexus.AiAnalysis.Worker.Retry;
using VitalNexus.AiAnalysis.Worker.Tests.Support;

namespace VitalNexus.AiAnalysis.Worker.Tests;

public class AiAnalysisQueueHandlerTests
{
    private readonly CapturingLogger<AiAnalysisQueueHandler> _logger = new();

    [Fact]
    public async Task HandleAsync_WithValidMessage_ReturnsCompleted()
    {
        var handler = CreateHandler();
        var message = CreateValidMessage();

        var result = await handler.HandleAsync(message);

        Assert.Equal(AiAnalysisQueueHandleStatus.Completed, result.Status);
        Assert.Equal(WorkerTestData.FakeRequestId, result.RequestId);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidMessage_ReturnsRejected()
    {
        var handler = CreateHandler();
        var message = new AiAnalysisQueueMessage(
            WorkerTestData.FakeRequestId,
            AnonymousPatientId: string.Empty,
            CreateValidMarkers());

        var result = await handler.HandleAsync(message);

        Assert.Equal(AiAnalysisQueueHandleStatus.Rejected, result.Status);
        Assert.NotNull(result.Detail);
    }

    [Fact]
    public async Task HandleAsync_WhenProcessingFailsAfterRetries_ReturnsFailed()
    {
        var handler = new AiAnalysisQueueHandler(
            new ThrowingProcessor(),
            CreateRetryExecutor(maxAttempts: 2),
            _logger);

        var result = await handler.HandleAsync(CreateValidMessage());

        Assert.Equal(AiAnalysisQueueHandleStatus.Failed, result.Status);
        Assert.Equal(WorkerTestData.FakeRequestId, result.RequestId);
    }

    [Fact]
    public async Task HandleAsync_NeverLogsMarkerNamesOrValues()
    {
        var handler = CreateHandler();
        var message = new AiAnalysisQueueMessage(
            WorkerTestData.FakeRequestId,
            WorkerTestData.FakeAnonymousPatientId,
            new[]
            {
                new AiAnalysisMarker(
                    WorkerTestData.FakeSensitiveMarkerName,
                    WorkerTestData.FakeSensitiveMarkerValue,
                    "mg/dL",
                    ReferenceLow: 2.0m,
                    ReferenceHigh: 5.0m),
            });

        await handler.HandleAsync(message);

        Assert.NotEmpty(_logger.Messages);
        Assert.All(_logger.Messages, messageText =>
        {
            Assert.DoesNotContain(WorkerTestData.FakeSensitiveMarkerName, messageText);
            Assert.DoesNotContain(WorkerTestData.FakeSensitiveMarkerValue.ToString(), messageText);
        });
    }

    private AiAnalysisQueueHandler CreateHandler()
    {
        var integrationBoundary = new FakeAiIntegrationBoundary();
        var resultStore = new FakeAiAnalysisResultStore();
        var processor = new AiAnalysisProcessor(
            integrationBoundary,
            resultStore,
            new CapturingLogger<AiAnalysisProcessor>());

        return new AiAnalysisQueueHandler(processor, CreateRetryExecutor(), _logger);
    }

    private static WorkerRetryExecutor CreateRetryExecutor(int maxAttempts = 3) =>
        new(
            Options.Create(new WorkerRetryOptions
            {
                MaxAttempts = maxAttempts,
                InitialDelaySeconds = 0,
                MaxDelaySeconds = 0,
            }),
            new CapturingLogger<WorkerRetryExecutor>());

    private static AiAnalysisQueueMessage CreateValidMessage() =>
        new(WorkerTestData.FakeRequestId, WorkerTestData.FakeAnonymousPatientId, CreateValidMarkers());

    private static AiAnalysisMarker[] CreateValidMarkers() =>
        new[]
        {
            new AiAnalysisMarker(
                WorkerTestData.FakeMarkerName,
                Value: 3.0m,
                "mg/dL",
                ReferenceLow: 2.0m,
                ReferenceHigh: 5.0m),
        };

    private sealed class ThrowingProcessor : IAiAnalysisProcessor
    {
        public Task<AiAnalysisResult> ProcessAsync(
            AiAnalysisRequest request,
            CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("Simulated transient failure.");
    }
}

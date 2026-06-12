using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VitalNexus.AiAnalysis.Worker.Configuration;
using VitalNexus.AiAnalysis.Worker.Handlers;
using VitalNexus.AiAnalysis.Worker.Models;
using VitalNexus.AiAnalysis.Worker.Processing;
using VitalNexus.AiAnalysis.Worker.Retry;
using VitalNexus.AiAnalysis.Worker.Tests.Support;

namespace VitalNexus.AiAnalysis.Worker.Tests;

public class AiAnalysisProcessorTests
{
    private readonly FakeAiIntegrationBoundary _integrationBoundary = new();
    private readonly FakeAiAnalysisResultStore _resultStore = new();
    private readonly CapturingLogger<AiAnalysisProcessor> _logger = new();

    [Fact]
    public async Task ProcessAsync_WithValidMessage_CompletesUsingMockedAiBoundary()
    {
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", null);
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);

        var processor = CreateProcessor();
        var request = CreateValidRequest();

        var result = await processor.ProcessAsync(request);

        Assert.Equal(AiAnalysisStatus.Completed, result.Status);
        Assert.Equal(1, _integrationBoundary.AnalyzeCallCount);
        Assert.Single(_resultStore.PersistedResults);
    }

    [Fact]
    public async Task ProcessAsync_WithInvalidMessage_ReturnsRejected()
    {
        var processor = CreateProcessor();
        var request = new AiAnalysisRequest(
            WorkerTestData.FakeRequestId,
            AnonymousPatientId: "   ",
            Markers: CreateValidMarkers());

        var result = await processor.ProcessAsync(request);

        Assert.Equal(AiAnalysisStatus.Rejected, result.Status);
        Assert.Equal(0, _integrationBoundary.AnalyzeCallCount);
    }

    [Fact]
    public async Task ProcessAsync_NeverLogsMarkerNamesOrValues()
    {
        var processor = CreateProcessor();
        var request = new AiAnalysisRequest(
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

        await processor.ProcessAsync(request);

        Assert.NotEmpty(_logger.Messages);
        Assert.All(_logger.Messages, message =>
        {
            Assert.DoesNotContain(WorkerTestData.FakeSensitiveMarkerName, message);
            Assert.DoesNotContain(WorkerTestData.FakeSensitiveMarkerValue.ToString(), message);
        });
    }

    private AiAnalysisProcessor CreateProcessor() =>
        new(_integrationBoundary, _resultStore, _logger);

    private static AiAnalysisRequest CreateValidRequest() =>
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
}

using Microsoft.Extensions.Logging;
using VitalNexus.Functions;
using VitalNexus.Functions.Models;

namespace VitalNexus.UnitTests.Functions;

public class AiAnalysisFunctionTests
{
    // All identifiers and values below are clearly fake test data — never real PHI.
    private const string FakeRequestId = "REQ-TEST-0001";
    private const string FakeAnonymousPatientId = "ANON-TEST-0001";

    private readonly CapturingLogger _logger = new();
    private readonly AiAnalysisFunction _function;

    public AiAnalysisFunctionTests() => _function = new AiAnalysisFunction(_logger);

    [Fact]
    public void ProcessRequest_FlagsOnlyOutOfRangeMarkers()
    {
        var request = new AiAnalysisRequest(FakeRequestId, FakeAnonymousPatientId, new[]
        {
            new AiAnalysisMarker("FAKE-MARKER-LOW", Value: 1.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
            new AiAnalysisMarker("FAKE-MARKER-HIGH", Value: 9.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
            new AiAnalysisMarker("FAKE-MARKER-OK", Value: 3.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
        });

        var result = _function.ProcessRequest(request);

        Assert.Equal(AiAnalysisStatus.Completed, result.Status);
        Assert.Equal(3, result.MarkersEvaluated);
        Assert.Equal(new[] { "FAKE-MARKER-LOW", "FAKE-MARKER-HIGH" }, result.FlaggedMarkers);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public void ProcessRequest_TreatsReferenceBoundaryValuesAsInRange()
    {
        var request = new AiAnalysisRequest(FakeRequestId, FakeAnonymousPatientId, new[]
        {
            new AiAnalysisMarker("FAKE-MARKER-AT-LOW", Value: 2.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
            new AiAnalysisMarker("FAKE-MARKER-AT-HIGH", Value: 5.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
        });

        var result = _function.ProcessRequest(request);

        Assert.Equal(AiAnalysisStatus.Completed, result.Status);
        Assert.Empty(result.FlaggedMarkers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ProcessRequest_WithoutAnonymousPatientId_IsRejected(string anonymousPatientId)
    {
        var request = new AiAnalysisRequest(FakeRequestId, anonymousPatientId, new[]
        {
            new AiAnalysisMarker("FAKE-MARKER-OK", Value: 3.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
        });

        var result = _function.ProcessRequest(request);

        Assert.Equal(AiAnalysisStatus.Rejected, result.Status);
        Assert.Equal(0, result.MarkersEvaluated);
        Assert.NotNull(result.RejectionReason);
    }

    [Fact]
    public void ProcessRequest_WithNoMarkers_IsRejected()
    {
        var request = new AiAnalysisRequest(FakeRequestId, FakeAnonymousPatientId, Array.Empty<AiAnalysisMarker>());

        var result = _function.ProcessRequest(request);

        Assert.Equal(AiAnalysisStatus.Rejected, result.Status);
        Assert.NotNull(result.RejectionReason);
    }

    [Fact]
    public void ProcessRequest_WithMalformedReferenceRange_IsRejected()
    {
        var request = new AiAnalysisRequest(FakeRequestId, FakeAnonymousPatientId, new[]
        {
            new AiAnalysisMarker("FAKE-MARKER-BAD-RANGE", Value: 3.0m, "mg/dL", ReferenceLow: 5.0m, ReferenceHigh: 2.0m),
        });

        var result = _function.ProcessRequest(request);

        Assert.Equal(AiAnalysisStatus.Rejected, result.Status);
        Assert.NotNull(result.RejectionReason);
    }

    [Fact]
    public void ProcessRequest_IsIdempotent_SameInputYieldsSameResult()
    {
        var request = new AiAnalysisRequest(FakeRequestId, FakeAnonymousPatientId, new[]
        {
            new AiAnalysisMarker("FAKE-MARKER-HIGH", Value: 9.0m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
        });

        var first = _function.ProcessRequest(request);
        var second = _function.ProcessRequest(request);

        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.MarkersEvaluated, second.MarkersEvaluated);
        Assert.Equal(first.FlaggedMarkers, second.FlaggedMarkers);
    }

    [Fact]
    public void ProcessRequest_NeverLogsMarkerNamesOrValues()
    {
        var request = new AiAnalysisRequest(FakeRequestId, FakeAnonymousPatientId, new[]
        {
            new AiAnalysisMarker("FAKE-MARKER-SENSITIVE", Value: 123.45m, "mg/dL", ReferenceLow: 2.0m, ReferenceHigh: 5.0m),
        });

        _function.ProcessRequest(request);

        Assert.NotEmpty(_logger.Messages);
        Assert.All(_logger.Messages, message =>
        {
            Assert.DoesNotContain("FAKE-MARKER-SENSITIVE", message);
            Assert.DoesNotContain("123.45", message);
        });
    }

    private sealed class CapturingLogger : ILogger<AiAnalysisFunction>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}

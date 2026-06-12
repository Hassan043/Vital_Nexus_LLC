namespace VitalNexus.AiAnalysis.Worker.Tests.Support;

/// <summary>Clearly fake identifiers — never real PHI.</summary>
internal static class WorkerTestData
{
    public const string FakeRequestId = "REQ-TEST-0001";
    public const string FakeAnonymousPatientId = "ANON-TEST-0001";
    public const string FakeMarkerName = "FAKE-MARKER-OK";
    public const string FakeSensitiveMarkerName = "FAKE-MARKER-SENSITIVE";
    public const decimal FakeSensitiveMarkerValue = 123.45m;
}

using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using VitalNexus.Functions.Models;

namespace VitalNexus.Functions;

/// <summary>
/// Phase 1 baseline for the AI analysis background job. The HTTP entry point
/// is a placeholder until the analysis queue arrives in Phase 4; the request
/// processing logic is real and unit-tested.
/// Processing is idempotent and safe to retry: the same request always yields
/// the same result. Logs carry only operation identifiers and counts — never
/// marker names, lab values, AI prompts/responses, or any PHI.
/// </summary>
public sealed class AiAnalysisFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<AiAnalysisFunction> _logger;

    public AiAnalysisFunction(ILogger<AiAnalysisFunction> logger) => _logger = logger;

    [Function("AiAnalysis")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ai-analysis")] HttpRequestData req)
    {
        AiAnalysisRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<AiAnalysisRequest>(req.Body, JsonOptions);
        }
        catch (JsonException)
        {
            request = null;
        }

        if (request is null)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            bad.Headers.Add("Content-Type", "application/json");
            await bad.WriteStringAsync("{\"error\":\"Invalid AI analysis request payload.\"}");
            return bad;
        }

        var result = ProcessRequest(request);

        var response = req.CreateResponse(
            result.Status == AiAnalysisStatus.Completed ? HttpStatusCode.OK : HttpStatusCode.UnprocessableEntity);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(result, JsonOptions));
        return response;
    }

    /// <summary>
    /// Validates an anonymized analysis request and flags markers outside their
    /// reference range. Boundary values are considered in range.
    /// </summary>
    public AiAnalysisResult ProcessRequest(AiAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            return Rejected(request, "Missing request identifier.");
        }

        if (string.IsNullOrWhiteSpace(request.AnonymousPatientId))
        {
            return Rejected(request, "Missing anonymous patient identifier.");
        }

        if (request.Markers is not { Count: > 0 })
        {
            return Rejected(request, "No markers to evaluate.");
        }

        if (request.Markers.Any(m => string.IsNullOrWhiteSpace(m.Name) || m.ReferenceLow > m.ReferenceHigh))
        {
            return Rejected(request, "One or more markers are malformed.");
        }

        var flagged = request.Markers
            .Where(m => m.Value < m.ReferenceLow || m.Value > m.ReferenceHigh)
            .Select(m => m.Name)
            .ToArray();

        _logger.LogInformation(
            "AI analysis request {RequestId} for patient {AnonymousPatientId} completed: {MarkerCount} markers evaluated, {FlaggedCount} flagged.",
            request.RequestId,
            request.AnonymousPatientId,
            request.Markers.Count,
            flagged.Length);

        return new AiAnalysisResult(
            request.RequestId,
            AiAnalysisStatus.Completed,
            request.Markers.Count,
            flagged);
    }

    private AiAnalysisResult Rejected(AiAnalysisRequest request, string reason)
    {
        _logger.LogWarning(
            "AI analysis request {RequestId} rejected: {Reason}",
            request.RequestId,
            reason);

        return new AiAnalysisResult(
            request.RequestId,
            AiAnalysisStatus.Rejected,
            MarkersEvaluated: 0,
            FlaggedMarkers: Array.Empty<string>(),
            RejectionReason: reason);
    }
}

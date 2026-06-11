using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;

namespace VitalNexus.Functions;

public sealed class HealthPingFunction
{
    private readonly ILogger<HealthPingFunction> _logger;

    public HealthPingFunction(ILogger<HealthPingFunction> logger)
    {
        _logger = logger;
    }

    [Function("HealthPing")]
    public HttpResponseData Run([
        HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData req)
    {
        _logger.LogInformation("Health ping received.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString("{\"status\":\"Healthy\"}");
        return response;
    }
}
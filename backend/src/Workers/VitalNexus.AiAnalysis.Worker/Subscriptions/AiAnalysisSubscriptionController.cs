using Dapr;
using Microsoft.AspNetCore.Mvc;
using VitalNexus.AiAnalysis.Worker.Handlers;
using VitalNexus.AiAnalysis.Worker.Models;

namespace VitalNexus.AiAnalysis.Worker.Subscriptions;

/// <summary>
/// Dapr pub/sub subscription endpoints for background AI analysis work.
/// Topic and pub/sub component names must match deployed Dapr manifests.
/// </summary>
[ApiController]
[Route("dapr")]
public sealed class AiAnalysisSubscriptionController : ControllerBase
{
    public const string PubSubComponentName = "pubsub";
    public const string AiAnalysisTopicName = "ai-analysis-queue";

    private readonly IAiAnalysisQueueHandler _queueHandler;
    private readonly ILogger<AiAnalysisSubscriptionController> _logger;

    public AiAnalysisSubscriptionController(
        IAiAnalysisQueueHandler queueHandler,
        ILogger<AiAnalysisSubscriptionController> logger)
    {
        _queueHandler = queueHandler;
        _logger = logger;
    }

    [Topic(PubSubComponentName, AiAnalysisTopicName)]
    [HttpPost("ai-analysis-queue")]
    public async Task<IActionResult> OnAiAnalysisQueueMessageAsync(
        [FromBody] AiAnalysisQueueMessage message,
        CancellationToken cancellationToken)
    {
        if (message is null)
        {
            _logger.LogWarning("AI analysis queue message rejected: empty payload.");
            return BadRequest();
        }

        var result = await _queueHandler.HandleAsync(message, cancellationToken).ConfigureAwait(false);

        return result.Status switch
        {
            AiAnalysisQueueHandleStatus.Completed or AiAnalysisQueueHandleStatus.Rejected => Ok(),
            AiAnalysisQueueHandleStatus.Failed => StatusCode(StatusCodes.Status500InternalServerError),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PRGatekeeper.Agents;
using PRGatekeeper.Contracts;
using PRGatekeeper.Models;
using PRGatekeeper.Services;

namespace PRGatekeeper.Controllers;

/// <summary>
/// Main API controller for PR gatekeeper analysis.
/// Exposes endpoints for PR analysis, results retrieval, and execution history.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GatekeeperController : ControllerBase
{
    private readonly AgentFactory _agentFactory;
    private readonly IStateManager _stateManager;
    private readonly ILogger<GatekeeperController> _logger;

    public GatekeeperController(
        AgentFactory agentFactory,
        IStateManager stateManager,
        ILogger<GatekeeperController> logger)
    {
        _agentFactory = agentFactory;
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a pull request and returns gatekeeper decision.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<GatekeeperResponse>> AnalyzePullRequest(
        [FromBody] AnalyzePullRequestRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("🔍 Analyzing PR #{PrNumber}", request.PrNumber);

        try
        {
            // Convert request to contract
            var prContract = new PullRequestContract
            {
                PrId = $"pr-{request.PrNumber}",
                PrNumber = request.PrNumber,
                Title = request.Title,
                Description = request.Description,
                Author = request.Author,
                SourceBranch = request.SourceBranch,
                TargetBranch = request.TargetBranch,
                ChangedFiles = request.ChangedFiles,
                LineAdditions = request.LineAdditions,
                LineDeletions = request.LineDeletions,
                Reviewers = request.Reviewers,
                Labels = request.Labels,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Execute the gatekeeper pipeline
            var executionResult = await _agentFactory.ExecuteGatekeeperPipelineAsync(prContract);

            // Convert to API response
            var response = GatekeeperResponse.FromExecutionResult(executionResult);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing PR #{PrNumber}", request.PrNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Analysis failed",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the review results for a specific PR.
    /// </summary>
    [HttpGet("review/pr-{prNumber}")]
    public async Task<ActionResult<ReviewResultContract>> GetReviewResults(int prNumber)
    {
        var prId = $"pr-{prNumber}";
        var result = await _stateManager.GetStateAsync<ReviewResultContract>(
            prId, $"{ReviewerAgent.AgentName}:Result");

        if (result == null)
        {
            return NotFound($"No review results found for PR {prNumber}");
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the analysis results for a specific PR.
    /// </summary>
    [HttpGet("analysis/pr-{prNumber}")]
    public async Task<ActionResult<AnalysisResultContract>> GetAnalysisResults(int prNumber)
    {
        var prId = $"pr-{prNumber}";
        var result = await _stateManager.GetStateAsync<AnalysisResultContract>(
            prId, $"{CodeAnalyzerAgent.AgentName}:Result");

        if (result == null)
        {
            return NotFound($"No analysis results found for PR {prNumber}");
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets notifications for a specific PR.
    /// </summary>
    [HttpGet("notifications/pr-{prNumber}")]
    public async Task<ActionResult<List<NotificationContract>>> GetNotifications(int prNumber)
    {
        var prId = $"pr-{prNumber}";
        var result = await _stateManager.GetStateAsync<List<NotificationContract>>(
            prId, $"{NotificationAgent.AgentName}:Notifications");

        if (result == null || result.Count == 0)
        {
            return NotFound($"No notifications found for PR {prNumber}");
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets execution history for a specific PR.
    /// </summary>
    [HttpGet("history/pr-{prNumber}")]
    public async Task<ActionResult<List<ExecutionRecord>>> GetExecutionHistory(int prNumber)
    {
        var prId = $"pr-{prNumber}";
        var history = await _stateManager.GetExecutionHistoryAsync(prId);

        if (history.Count == 0)
        {
            return NotFound($"No execution history found for PR {prNumber}");
        }

        return Ok(history);
    }

    /// <summary>
    /// Gets all state for a specific PR.
    /// </summary>
    [HttpGet("state/pr-{prNumber}")]
    public async Task<ActionResult<Dictionary<string, object>>> GetPrState(int prNumber)
    {
        var prId = $"pr-{prNumber}";
        var state = await _stateManager.GetAllStateAsync(prId);

        if (state.Count == 0)
        {
            return NotFound($"No state found for PR {prNumber}");
        }

        return Ok(state);
    }

    /// <summary>
    /// Clears state for a specific PR.
    /// </summary>
    [HttpDelete("state/pr-{prNumber}")]
    public async Task<IActionResult> ClearPrState(int prNumber)
    {
        var prId = $"pr-{prNumber}";
        var stateExists = await _stateManager.GetAllStateAsync(prId);

        if (stateExists.Count == 0)
        {
            return NotFound($"No state found for PR {prNumber}");
        }

        await _stateManager.ClearStateAsync(prId);
        _logger.LogInformation("State cleared for PR {PrNumber}", prNumber);

        return NoContent();
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public ActionResult<HealthCheckResponse> HealthCheck()
    {
        return Ok(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = "Unknown";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
}

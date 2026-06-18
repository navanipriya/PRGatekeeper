using Microsoft.Extensions.Logging;
using PRGatekeeper.Contracts;
using PRGatekeeper.Services;

namespace PRGatekeeper.Agents;

/// <summary>
/// Factory and orchestrator for all agents.
/// Implements the deterministic execution loop for Agent-to-Agent (A2A) collaboration.
/// </summary>
public class AgentFactory
{
    private readonly ReviewerAgent _reviewerAgent;
    private readonly CodeAnalyzerAgent _analyzerAgent;
    private readonly NotificationAgent _notificationAgent;
    private readonly IStateManager _stateManager;
    private readonly ExecutionTracker _executionTracker;
    private readonly ILogger<AgentFactory> _logger;

    public AgentFactory(
        IStateManager stateManager,
        ExecutionTracker executionTracker,
        ILoggerFactory loggerFactory,
        ILogger<AgentFactory> logger)
    {
        _stateManager = stateManager;
        _executionTracker = executionTracker;
        _logger = logger;

        // Initialize agents
        _reviewerAgent = new ReviewerAgent(stateManager, loggerFactory.CreateLogger<ReviewerAgent>());
        _analyzerAgent = new CodeAnalyzerAgent(stateManager, loggerFactory.CreateLogger<CodeAnalyzerAgent>());
        _notificationAgent = new NotificationAgent(stateManager, loggerFactory.CreateLogger<NotificationAgent>());
    }

    /// <summary>
    /// Executes the complete deterministic gatekeeper pipeline.
    /// Implements A2A collaboration with ordered, repeatable execution.
    /// </summary>
    public async Task<GatekeeperExecutionResult> ExecuteGatekeeperPipelineAsync(PullRequestContract pr)
    {
        ArgumentNullException.ThrowIfNull(pr);

        var executionId = Guid.NewGuid().ToString();
        _logger.LogInformation(
            "🎯 Starting Gatekeeper Pipeline | ExecutionId: {ExecutionId} | PR: {PrNumber}",
            executionId, pr.PrNumber);

        var result = new GatekeeperExecutionResult
        {
            ExecutionId = executionId,
            PrId = pr.PrId,
            PrNumber = pr.PrNumber,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Phase 1: Reviewer Agent
            _logger.LogInformation("📋 [Phase 1/3] Executing Reviewer Agent");
            result.ReviewResult = await _executionTracker.TrackExecutionAsync(
                pr.PrId,
                ReviewerAgent.AgentName,
                async () => await _reviewerAgent.ReviewAsync(pr),
                _stateManager
            );

            // Phase 2: Code Analyzer Agent
            _logger.LogInformation("🔍 [Phase 2/3] Executing Code Analyzer Agent");
            result.AnalysisResult = await _executionTracker.TrackExecutionAsync(
                pr.PrId,
                CodeAnalyzerAgent.AgentName,
                async () => await _analyzerAgent.AnalyzeAsync(pr),
                _stateManager
            );

            // Phase 3: Notification Agent
            _logger.LogInformation("📢 [Phase 3/3] Executing Notification Agent");
            result.Notifications = await _executionTracker.TrackExecutionAsync(
                pr.PrId,
                NotificationAgent.AgentName,
                async () => await _notificationAgent.GenerateNotificationsAsync(
                    pr,
                    result.ReviewResult,
                    result.AnalysisResult),
                _stateManager
            );

            // Determine overall status
            DetermineOverallStatus(result);

            result.EndTime = DateTime.UtcNow;
            result.Success = true;

            // Log execution summary
            await LogExecutionSummary(result);

            _logger.LogInformation(
                "✅ Gatekeeper Pipeline completed | Status: {Status} | Duration: {Duration}ms",
                result.FinalStatus, (result.EndTime - result.StartTime).TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Gatekeeper Pipeline failed | ExecutionId: {ExecutionId}", executionId);

            result.EndTime = DateTime.UtcNow;
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.FinalStatus = "Failed";

            return result;
        }
    }

    /// <summary>
    /// Determines the final status based on individual agent results.
    /// Implements decision logic for A2A coordination.
    /// </summary>
    private void DetermineOverallStatus(GatekeeperExecutionResult result)
    {
        if (result.ReviewResult?.Status == ReviewStatus.Blocked)
        {
            result.FinalStatus = "Blocked";
            result.Recommendation = "PR is blocked and cannot proceed. Address critical issues.";
            return;
        }

        var criticalAnalysisIssues = result.AnalysisResult?.SecurityFindings
            .Where(f => f.CvssScore >= 7.0)
            .Any() ?? false;

        if (criticalAnalysisIssues)
        {
            result.FinalStatus = "NeedsSecurityReview";
            result.Recommendation = "Critical security issues detected. Requires security team review.";
            return;
        }

        var hasErrors = (result.ReviewResult?.Issues.Any(i => i.Severity == IssueSeverity.Error) ?? false) ||
                       (result.AnalysisResult?.QualityIssues.Any(i => i.Severity == "Error") ?? false);

        if (hasErrors)
        {
            result.FinalStatus = "ChangesRequested";
            result.Recommendation = "Fix identified errors before re-submission.";
            return;
        }

        if (result.ReviewResult?.ApprovalScore >= 80 && result.AnalysisResult?.OverallScore >= 70)
        {
            result.FinalStatus = "Approved";
            result.Recommendation = "PR is approved and ready to merge.";
            return;
        }

        result.FinalStatus = "NeedsReview";
        result.Recommendation = "PR needs additional review before approval.";
    }

    private async Task LogExecutionSummary(GatekeeperExecutionResult result)
    {
        var executionRecords = await _stateManager.GetExecutionHistoryAsync(result.PrId);
        var summary = _executionTracker.GenerateSummary(executionRecords);

        _logger.LogInformation(
            "📊 Execution Summary | Total: {Total}, Success: {Success}, Failed: {Failed}, " +
            "TotalDuration: {Duration}ms, AvgDuration: {AvgDuration}ms",
            summary.TotalExecutions, summary.SuccessfulExecutions, summary.FailedExecutions,
            summary.TotalDurationMs, summary.AverageDurationMs);

        foreach (var agentStats in summary.ExecutionsByAgent)
        {
            _logger.LogInformation(
                "  📈 {Agent}: Executions={Count}, Success={Success}, Failed={Failed}, AvgDuration={AvgDuration}ms",
                agentStats.Key, agentStats.Value.ExecutionCount, agentStats.Value.SuccessCount,
                agentStats.Value.FailureCount, agentStats.Value.AverageDurationMs);
        }
    }
}

/// <summary>
/// Result of a complete gatekeeper execution pipeline.
/// Encapsulates all A2A agent outputs.
/// </summary>
public class GatekeeperExecutionResult
{
    public string ExecutionId { get; set; } = string.Empty;
    public string PrId { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // A2A Collaboration Results
    public ReviewResultContract? ReviewResult { get; set; }
    public AnalysisResultContract? AnalysisResult { get; set; }
    public List<NotificationContract>? Notifications { get; set; }

    // Decision Output
    public string FinalStatus { get; set; } = "Unknown";
    public string Recommendation { get; set; } = string.Empty;

    public TimeSpan ExecutionDuration => EndTime - StartTime;
}

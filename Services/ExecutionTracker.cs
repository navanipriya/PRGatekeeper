using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace PRGatekeeper.Services;

/// <summary>
/// Tracks agent execution performance, timing, and generates execution reports.
/// </summary>
public class ExecutionTracker
{
    private readonly ILogger<ExecutionTracker> _logger;

    public ExecutionTracker(ILogger<ExecutionTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Wraps agent execution with timing and error tracking.
    /// </summary>
    public async Task<T> TrackExecutionAsync<T>(
        string prId,
        string agentName,
        Func<Task<T>> execution,
        IStateManager stateManager)
    {
        var executionId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("🚀 Starting execution: {ExecutionId} | Agent: {Agent} | PR: {PrId}",
                executionId, agentName, prId);

            var record = new ExecutionRecord
            {
                ExecutionId = executionId,
                AgentName = agentName,
                Phase = ExecutionPhase.Started,
                Status = "Started",
                Context = new()
                {
                    { "timestamp", DateTime.UtcNow },
                    { "environment", "production" }
                }
            };

            await stateManager.RecordExecutionAsync(prId, record);

            // Execute the agent
            var result = await execution();

            stopwatch.Stop();

            var completedRecord = new ExecutionRecord
            {
                ExecutionId = executionId,
                AgentName = agentName,
                Phase = ExecutionPhase.Completed,
                Status = "Success",
                DurationMs = stopwatch.ElapsedMilliseconds
            };

            await stateManager.RecordExecutionAsync(prId, completedRecord);

            _logger.LogInformation(
                "✅ Execution completed: {ExecutionId} | Agent: {Agent} | Duration: {Duration}ms",
                executionId, agentName, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "❌ Execution failed: {ExecutionId} | Agent: {Agent} | Duration: {Duration}ms",
                executionId, agentName, stopwatch.ElapsedMilliseconds);

            var failedRecord = new ExecutionRecord
            {
                ExecutionId = executionId,
                AgentName = agentName,
                Phase = ExecutionPhase.Failed,
                Status = "Failed",
                ErrorMessage = ex.Message,
                DurationMs = stopwatch.ElapsedMilliseconds
            };

            await stateManager.RecordExecutionAsync(prId, failedRecord);

            throw;
        }
    }

    /// <summary>
    /// Generates execution summary report.
    /// </summary>
    public ExecutionSummary GenerateSummary(List<ExecutionRecord> records)
    {
        var summary = new ExecutionSummary
        {
            TotalExecutions = records.Count,
            SuccessfulExecutions = records.Count(r => r.Phase == ExecutionPhase.Completed),
            FailedExecutions = records.Count(r => r.Phase == ExecutionPhase.Failed),
            TotalDurationMs = records.Sum(r => r.DurationMs),
            AverageDurationMs = records.Count > 0 ? records.Average(r => r.DurationMs) : 0,
            ExecutionsByAgent = records
                .GroupBy(r => r.AgentName)
                .ToDictionary(
                    g => g.Key,
                    g => new AgentExecutionStats
                    {
                        ExecutionCount = g.Count(),
                        SuccessCount = g.Count(r => r.Phase == ExecutionPhase.Completed),
                        FailureCount = g.Count(r => r.Phase == ExecutionPhase.Failed),
                        AverageDurationMs = g.Average(r => r.DurationMs)
                    })
        };

        return summary;
    }
}

public class ExecutionSummary
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public long TotalDurationMs { get; set; }
    public double AverageDurationMs { get; set; }
    public Dictionary<string, AgentExecutionStats> ExecutionsByAgent { get; set; } = new();
}

public class AgentExecutionStats
{
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double AverageDurationMs { get; set; }
}

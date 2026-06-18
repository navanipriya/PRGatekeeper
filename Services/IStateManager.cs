namespace PRGatekeeper.Services;

/// <summary>
/// Interface for managing agent state and execution context.
/// Enables deterministic and reproducible agent execution patterns.
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// Stores a state object associated with a PR.
    /// </summary>
    Task SetStateAsync<T>(string prId, string key, T value) where T : class;

    /// <summary>
    /// Retrieves a state object for a PR.
    /// </summary>
    Task<T?> GetStateAsync<T>(string prId, string key) where T : class;

    /// <summary>
    /// Retrieves all state for a PR.
    /// </summary>
    Task<Dictionary<string, object>> GetAllStateAsync(string prId);

    /// <summary>
    /// Removes state for a PR.
    /// </summary>
    Task ClearStateAsync(string prId);

    /// <summary>
    /// Checks if state exists.
    /// </summary>
    Task<bool> StateExistsAsync(string prId, string key);

    /// <summary>
    /// Records an execution event for audit trail.
    /// </summary>
    Task RecordExecutionAsync(string prId, ExecutionRecord record);

    /// <summary>
    /// Retrieves execution history for a PR.
    /// </summary>
    Task<List<ExecutionRecord>> GetExecutionHistoryAsync(string prId);
}

/// <summary>
/// Represents a single execution event for audit and debugging.
/// </summary>
public class ExecutionRecord
{
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    public string AgentName { get; set; } = string.Empty;
    public ExecutionPhase Phase { get; set; }
    public string Status { get; set; } = "Success";
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public enum ExecutionPhase
{
    Started,
    Processing,
    Completed,
    Failed,
    Retrying
}

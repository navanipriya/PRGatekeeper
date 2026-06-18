using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PRGatekeeper.Services;

/// <summary>
/// In-memory implementation of state management for agent execution.
/// Suitable for single-instance deployments; use distributed cache for multi-instance.
/// </summary>
public class InMemoryStateManager : IStateManager
{
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _stateStore = new();
    private readonly ConcurrentDictionary<string, List<ExecutionRecord>> _executionHistory = new();
    private readonly ILogger<InMemoryStateManager> _logger;

    public InMemoryStateManager(ILogger<InMemoryStateManager> logger)
    {
        _logger = logger;
    }

    public Task SetStateAsync<T>(string prId, string key, T value) where T : class
    {
        ArgumentNullException.ThrowIfNull(prId);
        ArgumentNullException.ThrowIfNull(key);

        var state = _stateStore.GetOrAdd(prId, _ => new Dictionary<string, object>());
        state[key] = value;

        _logger.LogDebug("State set for PR {PrId}: {Key}", prId, key);
        return Task.CompletedTask;
    }

    public Task<T?> GetStateAsync<T>(string prId, string key) where T : class
    {
        ArgumentNullException.ThrowIfNull(prId);
        ArgumentNullException.ThrowIfNull(key);

        if (_stateStore.TryGetValue(prId, out var state) && state.TryGetValue(key, out var value))
        {
            _logger.LogDebug("State retrieved for PR {PrId}: {Key}", prId, key);
            return Task.FromResult(value as T);
        }

        _logger.LogDebug("State not found for PR {PrId}: {Key}", prId, key);
        return Task.FromResult<T?>(null);
    }

    public Task<Dictionary<string, object>> GetAllStateAsync(string prId)
    {
        ArgumentNullException.ThrowIfNull(prId);

        if (_stateStore.TryGetValue(prId, out var state))
        {
            return Task.FromResult(new Dictionary<string, object>(state));
        }

        return Task.FromResult(new Dictionary<string, object>());
    }

    public Task ClearStateAsync(string prId)
    {
        ArgumentNullException.ThrowIfNull(prId);

        _stateStore.TryRemove(prId, out _);
        _executionHistory.TryRemove(prId, out _);

        _logger.LogInformation("State cleared for PR {PrId}", prId);
        return Task.CompletedTask;
    }

    public Task<bool> StateExistsAsync(string prId, string key)
    {
        ArgumentNullException.ThrowIfNull(prId);
        ArgumentNullException.ThrowIfNull(key);

        if (_stateStore.TryGetValue(prId, out var state))
        {
            return Task.FromResult(state.ContainsKey(key));
        }

        return Task.FromResult(false);
    }

    public Task RecordExecutionAsync(string prId, ExecutionRecord record)
    {
        ArgumentNullException.ThrowIfNull(prId);
        ArgumentNullException.ThrowIfNull(record);

        var history = _executionHistory.GetOrAdd(prId, _ => new List<ExecutionRecord>());
        lock (history)
        {
            history.Add(record);
        }

        _logger.LogInformation(
            "Execution recorded for PR {PrId}: Agent={Agent}, Phase={Phase}, Duration={Duration}ms",
            prId, record.AgentName, record.Phase, record.DurationMs);

        return Task.CompletedTask;
    }

    public Task<List<ExecutionRecord>> GetExecutionHistoryAsync(string prId)
    {
        ArgumentNullException.ThrowIfNull(prId);

        if (_executionHistory.TryGetValue(prId, out var history))
        {
            lock (history)
            {
                return Task.FromResult(new List<ExecutionRecord>(history));
            }
        }

        return Task.FromResult(new List<ExecutionRecord>());
    }
}

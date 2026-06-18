using System.Text.Json.Serialization;

namespace PRGatekeeper.Contracts;

/// <summary>
/// Contract for notifications sent by the Notification Agent.
/// </summary>
public class NotificationContract
{
    [JsonPropertyName("notificationId")]
    public string NotificationId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("prId")]
    public string PrId { get; set; } = string.Empty;

    [JsonPropertyName("prNumber")]
    public int PrNumber { get; set; }

    [JsonPropertyName("type")]
    public NotificationType Type { get; set; } = NotificationType.Analysis;

    [JsonPropertyName("severity")]
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; set; } = new();

    [JsonPropertyName("channels")]
    public List<NotificationChannel> Channels { get; set; } = new();

    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; } = 0;

    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; set; } = "1.0";
}

public enum NotificationType
{
    Analysis,
    Review,
    Approval,
    Blocking,
    Alert
}

public enum NotificationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum NotificationChannel
{
    Email,
    Slack,
    Teams,
    GitHub,
    Webhook
}

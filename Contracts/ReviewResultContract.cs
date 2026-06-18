using System.Text.Json.Serialization;

namespace PRGatekeeper.Contracts;

/// <summary>
/// Contract for PR review results produced by the Reviewer Agent.
/// </summary>
public class ReviewResultContract
{
    [JsonPropertyName("reviewId")]
    public string ReviewId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("prId")]
    public string PrId { get; set; } = string.Empty;

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = "ReviewerAgent";

    [JsonPropertyName("status")]
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    [JsonPropertyName("validationChecks")]
    public List<ValidationCheck> ValidationChecks { get; set; } = new();

    [JsonPropertyName("issues")]
    public List<ReviewIssue> Issues { get; set; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();

    [JsonPropertyName("riskLevel")]
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;

    [JsonPropertyName("approvalScore")]
    public double ApprovalScore { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; set; } = "1.0";
}

public enum ReviewStatus
{
    Pending,
    InProgress,
    Approved,
    RequestedChanges,
    Blocked
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public class ValidationCheck
{
    [JsonPropertyName("checkName")]
    public string CheckName { get; set; } = string.Empty;

    [JsonPropertyName("passed")]
    public bool Passed { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ReviewIssue
{
    [JsonPropertyName("issueId")]
    public string IssueId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("severity")]
    public IssueSeverity Severity { get; set; } = IssueSeverity.Info;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string? File { get; set; }

    [JsonPropertyName("lineNumber")]
    public int? LineNumber { get; set; }
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

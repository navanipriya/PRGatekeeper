using System.Text.Json.Serialization;

namespace PRGatekeeper.Contracts;

/// <summary>
/// Structured contract for pull request data passed between agents.
/// Ensures type safety and deterministic serialization across agent boundaries.
/// </summary>
public class PullRequestContract
{
    [JsonPropertyName("prId")]
    public string PrId { get; set; } = string.Empty;

    [JsonPropertyName("prNumber")]
    public int PrNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("targetBranch")]
    public string TargetBranch { get; set; } = "main";

    [JsonPropertyName("sourceBranch")]
    public string SourceBranch { get; set; } = string.Empty;

    [JsonPropertyName("changedFiles")]
    public List<string> ChangedFiles { get; set; } = new();

    [JsonPropertyName("lineAdditions")]
    public int LineAdditions { get; set; }

    [JsonPropertyName("lineDeletions")]
    public int LineDeletions { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();

    [JsonPropertyName("reviewers")]
    public List<string> Reviewers { get; set; } = new();

    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; set; } = "1.0";
}

using System.ComponentModel.DataAnnotations;

namespace PRGatekeeper.Models;

/// <summary>
/// Request model for analyzing a pull request.
/// </summary>
public class AnalyzePullRequestRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int PrNumber { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public List<string> ChangedFiles { get; set; } = new();

    [Range(0, int.MaxValue)]
    public int LineAdditions { get; set; }

    [Range(0, int.MaxValue)]
    public int LineDeletions { get; set; }

    [StringLength(100)]
    public string Author { get; set; } = "unknown";

    [StringLength(100)]
    public string SourceBranch { get; set; } = "feature/default";

    [StringLength(100)]
    public string TargetBranch { get; set; } = "main";

    public List<string> Reviewers { get; set; } = new();

    public List<string> Labels { get; set; } = new();
}

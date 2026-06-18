using Microsoft.Extensions.Logging;
using PRGatekeeper.Contracts;
using PRGatekeeper.Services;

namespace PRGatekeeper.Agents;

/// <summary>
/// Reviewer Agent - Validates PR structure, metadata, and review criteria.
/// Produces ReviewResultContract with deterministic output.
/// </summary>
public class ReviewerAgent
{
    private readonly IStateManager _stateManager;
    private readonly ILogger<ReviewerAgent> _logger;

    public const string AgentName = "ReviewerAgent";

    public ReviewerAgent(IStateManager stateManager, ILogger<ReviewerAgent> logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Executes the reviewer agent on a PR contract.
    /// </summary>
    public async Task<ReviewResultContract> ReviewAsync(PullRequestContract pr)
    {
        ArgumentNullException.ThrowIfNull(pr);

        _logger.LogInformation("📋 Reviewer Agent processing PR: {PrNumber}", pr.PrNumber);

        var result = new ReviewResultContract
        {
            PrId = pr.PrId,
            Status = ReviewStatus.InProgress
        };

        // Validate PR structure
        ValidatePrStructure(pr, result);

        // Validate change scope
        ValidateChangeScope(pr, result);

        // Validate description quality
        ValidateDescriptionQuality(pr, result);

        // Calculate approval score
        CalculateApprovalScore(result);

        // Determine final status
        DetermineFinalStatus(result);

        // Store result in state for A2A communication
        await _stateManager.SetStateAsync(pr.PrId, $"{AgentName}:Result", result);

        _logger.LogInformation(
            "✅ Review completed for PR {PrNumber}: Status={Status}, Score={Score}",
            pr.PrNumber, result.Status, result.ApprovalScore);

        return result;
    }

    private void ValidatePrStructure(PullRequestContract pr, ReviewResultContract result)
    {
        var checks = new List<ValidationCheck>();

        // Check 1: Title present and meaningful
        checks.Add(new ValidationCheck
        {
            CheckName = "PR Title",
            Passed = !string.IsNullOrWhiteSpace(pr.Title) && pr.Title.Length >= 10,
            Message = string.IsNullOrWhiteSpace(pr.Title)
                ? "PR title is empty"
                : pr.Title.Length < 10
                    ? "PR title is too short (minimum 10 characters)"
                    : "PR title is present and meaningful"
        });

        // Check 2: Description present
        checks.Add(new ValidationCheck
        {
            CheckName = "PR Description",
            Passed = !string.IsNullOrWhiteSpace(pr.Description) && pr.Description.Length >= 20,
            Message = string.IsNullOrWhiteSpace(pr.Description)
                ? "PR description is empty"
                : pr.Description.Length < 20
                    ? "PR description is too short (minimum 20 characters)"
                    : "PR description is present"
        });

        // Check 3: Branch naming
        var validBranchPattern = pr.SourceBranch.StartsWith("feature/", StringComparison.OrdinalIgnoreCase)
            || pr.SourceBranch.StartsWith("bugfix/", StringComparison.OrdinalIgnoreCase)
            || pr.SourceBranch.StartsWith("hotfix/", StringComparison.OrdinalIgnoreCase)
            || pr.SourceBranch == "develop";

        checks.Add(new ValidationCheck
        {
            CheckName = "Branch Naming",
            Passed = validBranchPattern,
            Message = validBranchPattern ? "Branch name follows conventions" : "Branch name doesn't follow naming conventions"
        });

        // Check 4: Has at least one reviewer
        checks.Add(new ValidationCheck
        {
            CheckName = "Reviewers Assigned",
            Passed = pr.Reviewers.Count > 0,
            Message = pr.Reviewers.Count > 0 ? "Reviewers assigned" : "No reviewers assigned"
        });

        result.ValidationChecks = checks;

        // Add issues for failed checks
        foreach (var check in checks.Where(c => !c.Passed))
        {
            result.Issues.Add(new ReviewIssue
            {
                Severity = IssueSeverity.Warning,
                Category = check.CheckName,
                Message = check.Message
            });
        }
    }

    private void ValidateChangeScope(PullRequestContract pr, ReviewResultContract result)
    {
        _logger.LogDebug("Validating change scope for PR {PrNumber}", pr.PrNumber);

        var totalChanges = pr.LineAdditions + pr.LineDeletions;
        var fileCount = pr.ChangedFiles.Count;

        // Large PRs need justification
        if (totalChanges > 500)
        {
            result.Issues.Add(new ReviewIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Change Scope",
                Message = $"Large PR: {totalChanges} total lines changed across {fileCount} files. Consider splitting into smaller PRs."
            });
            result.RiskLevel = RiskLevel.High;
        }

        // Too many files changed
        if (fileCount > 50)
        {
            result.Issues.Add(new ReviewIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Change Scope",
                Message = $"PR touches {fileCount} files. Large surface area increases review complexity."
            });
            result.RiskLevel = RiskLevel.High;
        }

        result.Recommendations.Add($"Ensure all {fileCount} changed files are necessary for this feature");
    }

    private void ValidateDescriptionQuality(PullRequestContract pr, ReviewResultContract result)
    {
        _logger.LogDebug("Validating description quality for PR {PrNumber}", pr.PrNumber);

        var description = pr.Description.ToLowerInvariant();

        // Check for motivation
        if (!description.Contains("motivation") && !description.Contains("why") && !description.Contains("purpose"))
        {
            result.Recommendations.Add("Add explanation of motivation and purpose to description");
        }

        // Check for testing
        if (!description.Contains("test") && !description.Contains("tested"))
        {
            result.Issues.Add(new ReviewIssue
            {
                Severity = IssueSeverity.Warning,
                Category = "Description Quality",
                Message = "Description doesn't mention testing approach"
            });
        }

        // Check for breaking changes
        if (description.Contains("breaking") || description.Contains("migration"))
        {
            result.RiskLevel = RiskLevel.Critical;
            result.Recommendations.Add("Migration guide or breaking change documentation required");
        }
    }

    private void CalculateApprovalScore(ReviewResultContract result)
    {
        // Start with 100 points
        double score = 100.0;

        // Deduct points for issues
        score -= result.Issues.Count(i => i.Severity == IssueSeverity.Info) * 5;
        score -= result.Issues.Count(i => i.Severity == IssueSeverity.Warning) * 15;
        score -= result.Issues.Count(i => i.Severity == IssueSeverity.Error) * 25;
        score -= result.Issues.Count(i => i.Severity == IssueSeverity.Critical) * 50;

        // Factor in risk level
        score = result.RiskLevel switch
        {
            RiskLevel.Critical => Math.Max(score - 40, 0),
            RiskLevel.High => Math.Max(score - 20, 0),
            RiskLevel.Medium => Math.Max(score - 10, 0),
            _ => score
        };

        result.ApprovalScore = Math.Max(0, Math.Min(score, 100));
    }

    private void DetermineFinalStatus(ReviewResultContract result)
    {
        if (result.RiskLevel == RiskLevel.Critical)
        {
            result.Status = ReviewStatus.Blocked;
        }
        else if (result.ApprovalScore >= 80 && !result.Issues.Any(i => i.Severity == IssueSeverity.Error))
        {
            result.Status = ReviewStatus.Approved;
        }
        else if (result.Issues.Any(i => i.Severity == IssueSeverity.Error))
        {
            result.Status = ReviewStatus.RequestedChanges;
        }
        else
        {
            result.Status = ReviewStatus.Approved;
        }
    }
}

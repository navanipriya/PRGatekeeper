using PRGatekeeper.Agents;
using PRGatekeeper.Contracts;

namespace PRGatekeeper.Models;

/// <summary>
/// API response model for gatekeeper analysis.
/// </summary>
public class GatekeeperResponse
{
    public string ExecutionId { get; set; } = string.Empty;
    public int PrNumber { get; set; }
    public string FinalStatus { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long ExecutionTimeMs { get; set; }

    // Scores and Results
    public ReviewSummary? Review { get; set; }
    public AnalysisSummary? Analysis { get; set; }
    public int NotificationCount { get; set; }

    public static GatekeeperResponse FromExecutionResult(GatekeeperExecutionResult result)
    {
        return new GatekeeperResponse
        {
            ExecutionId = result.ExecutionId,
            PrNumber = result.PrNumber,
            FinalStatus = result.FinalStatus,
            Recommendation = result.Recommendation,
            Timestamp = result.EndTime,
            ExecutionTimeMs = (long)result.ExecutionDuration.TotalMilliseconds,
            Review = result.ReviewResult != null ? ReviewSummary.FromContract(result.ReviewResult) : null,
            Analysis = result.AnalysisResult != null ? AnalysisSummary.FromContract(result.AnalysisResult) : null,
            NotificationCount = result.Notifications?.Count ?? 0
        };
    }
}

public class ReviewSummary
{
    public string Status { get; set; } = string.Empty;
    public double ApprovalScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public int IssueCount { get; set; }
    public int RecommendationCount { get; set; }

    public static ReviewSummary FromContract(ReviewResultContract contract)
    {
        return new ReviewSummary
        {
            Status = contract.Status.ToString(),
            ApprovalScore = contract.ApprovalScore,
            RiskLevel = contract.RiskLevel.ToString(),
            IssueCount = contract.Issues.Count,
            RecommendationCount = contract.Recommendations.Count
        };
    }
}

public class AnalysisSummary
{
    public double QualityScore { get; set; }
    public double SecurityScore { get; set; }
    public double OverallScore { get; set; }
    public int QualityIssueCount { get; set; }
    public int SecurityFindingCount { get; set; }
    public int PerformanceWarningCount { get; set; }

    public static AnalysisSummary FromContract(AnalysisResultContract contract)
    {
        return new AnalysisSummary
        {
            QualityScore = contract.QualityScore,
            SecurityScore = contract.SecurityScore,
            OverallScore = contract.OverallScore,
            QualityIssueCount = contract.QualityIssues.Count,
            SecurityFindingCount = contract.SecurityFindings.Count,
            PerformanceWarningCount = contract.PerformanceWarnings.Count
        };
    }
}

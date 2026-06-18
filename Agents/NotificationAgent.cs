using Microsoft.Extensions.Logging;
using PRGatekeeper.Contracts;
using PRGatekeeper.Services;

namespace PRGatekeeper.Agents;

/// <summary>
/// Notification Agent - Dispatches notifications based on analysis results.
/// Coordinates with Reviewer and Analyzer agents to determine appropriate notifications.
/// </summary>
public class NotificationAgent
{
    private readonly IStateManager _stateManager;
    private readonly ILogger<NotificationAgent> _logger;

    public const string AgentName = "NotificationAgent";

    public NotificationAgent(IStateManager stateManager, ILogger<NotificationAgent> logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Generates notifications based on review and analysis results.
    /// </summary>
    public async Task<List<NotificationContract>> GenerateNotificationsAsync(
        PullRequestContract pr,
        ReviewResultContract? reviewResult,
        AnalysisResultContract? analysisResult)
    {
        ArgumentNullException.ThrowIfNull(pr);

        _logger.LogInformation("📢 Notification Agent processing PR: {PrNumber}", pr.PrNumber);

        var notifications = new List<NotificationContract>();

        // Generate review notifications
        if (reviewResult != null)
        {
            var reviewNotifications = GenerateReviewNotifications(pr, reviewResult);
            notifications.AddRange(reviewNotifications);
        }

        // Generate analysis notifications
        if (analysisResult != null)
        {
            var analysisNotifications = GenerateAnalysisNotifications(pr, analysisResult);
            notifications.AddRange(analysisNotifications);
        }

        // Generate summary notification if there are critical issues
        if (notifications.Any(n => n.Severity == NotificationSeverity.Critical))
        {
            var summaryNotification = GenerateCriticalSummaryNotification(pr, notifications);
            notifications.Add(summaryNotification);
        }

        // Store notifications in state
        await _stateManager.SetStateAsync(pr.PrId, $"{AgentName}:Notifications", notifications);

        _logger.LogInformation(
            "✅ Generated {Count} notifications for PR {PrNumber}",
            notifications.Count, pr.PrNumber);

        return notifications;
    }

    private List<NotificationContract> GenerateReviewNotifications(
        PullRequestContract pr,
        ReviewResultContract reviewResult)
    {
        var notifications = new List<NotificationContract>();

        // Approval notification
        if (reviewResult.Status == ReviewStatus.Approved)
        {
            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Approval,
                Severity = NotificationSeverity.Info,
                Title = "PR Approved by Reviewer",
                Message = $"PR #{pr.PrNumber} '{pr.Title}' has been approved with a score of {reviewResult.ApprovalScore:F0}/100",
                Recipients = pr.Reviewers,
                Channels = new() { NotificationChannel.GitHub, NotificationChannel.Slack },
                Data = new()
                {
                    { "approvalScore", reviewResult.ApprovalScore },
                    { "riskLevel", reviewResult.RiskLevel.ToString() }
                }
            });
        }

        // Changes requested notification
        if (reviewResult.Status == ReviewStatus.RequestedChanges)
        {
            var criticalIssues = reviewResult.Issues.Where(i => i.Severity == IssueSeverity.Error).ToList();

            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Review,
                Severity = NotificationSeverity.Warning,
                Title = "Review: Changes Requested",
                Message = $"PR #{pr.PrNumber} requires {criticalIssues.Count} critical changes before approval",
                Recipients = new() { pr.Author },
                Channels = new() { NotificationChannel.GitHub, NotificationChannel.Email },
                Data = new()
                {
                    { "issueCount", reviewResult.Issues.Count },
                    { "criticalCount", criticalIssues.Count }
                }
            });
        }

        // Blocked notification
        if (reviewResult.Status == ReviewStatus.Blocked)
        {
            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Blocking,
                Severity = NotificationSeverity.Critical,
                Title = "⛔ PR Blocked",
                Message = $"PR #{pr.PrNumber} is blocked due to {reviewResult.RiskLevel} risk level",
                Recipients = new() { pr.Author },
                Channels = new() { NotificationChannel.GitHub, NotificationChannel.Email, NotificationChannel.Teams }
            });
        }

        return notifications;
    }

    private List<NotificationContract> GenerateAnalysisNotifications(
        PullRequestContract pr,
        AnalysisResultContract analysisResult)
    {
        var notifications = new List<NotificationContract>();

        // Security findings notification
        if (analysisResult.SecurityFindings.Count > 0)
        {
            var criticalFindings = analysisResult.SecurityFindings.Where(f => f.CvssScore >= 7.0).ToList();

            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Alert,
                Severity = criticalFindings.Any() ? NotificationSeverity.Critical : NotificationSeverity.Warning,
                Title = "🔒 Security Analysis Results",
                Message = $"PR #{pr.PrNumber} has {analysisResult.SecurityFindings.Count} security finding(s) " +
                         $"({criticalFindings.Count} critical)",
                Recipients = new() { "security-team@company.com" },
                Channels = new() { NotificationChannel.Email, NotificationChannel.Teams, NotificationChannel.Slack },
                Data = new()
                {
                    { "totalFindings", analysisResult.SecurityFindings.Count },
                    { "criticalFindings", criticalFindings.Count },
                    { "securityScore", analysisResult.SecurityScore }
                }
            });
        }

        // Quality issues notification
        var errorCount = analysisResult.QualityIssues.Count(i => i.Severity == "Error");
        if (errorCount > 0)
        {
            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Analysis,
                Severity = NotificationSeverity.Warning,
                Title = "Code Quality Issues Found",
                Message = $"PR #{pr.PrNumber} has {errorCount} code quality error(s) to address",
                Recipients = new() { pr.Author },
                Channels = new() { NotificationChannel.GitHub, NotificationChannel.Slack },
                Data = new()
                {
                    { "errorCount", errorCount },
                    { "warningCount", analysisResult.QualityIssues.Count(i => i.Severity == "Warning") },
                    { "qualityScore", analysisResult.QualityScore }
                }
            });
        }

        // Performance warnings notification
        if (analysisResult.PerformanceWarnings.Count > 0)
        {
            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Alert,
                Severity = NotificationSeverity.Warning,
                Title = "⚡ Performance Concerns",
                Message = $"PR #{pr.PrNumber} has {analysisResult.PerformanceWarnings.Count} potential performance issue(s)",
                Recipients = pr.Reviewers,
                Channels = new() { NotificationChannel.Slack },
                Data = new()
                {
                    { "warnings", string.Join("; ", analysisResult.PerformanceWarnings) }
                }
            });
        }

        // Overall score notification (for tracking)
        if (analysisResult.OverallScore < 60)
        {
            notifications.Add(new NotificationContract
            {
                PrId = pr.PrId,
                PrNumber = pr.PrNumber,
                Type = NotificationType.Analysis,
                Severity = NotificationSeverity.Warning,
                Title = "Low Quality Score",
                Message = $"PR #{pr.PrNumber} quality score is below threshold: {analysisResult.OverallScore:F0}/100",
                Recipients = pr.Reviewers,
                Channels = new() { NotificationChannel.GitHub }
            });
        }

        return notifications;
    }

    private NotificationContract GenerateCriticalSummaryNotification(
        PullRequestContract pr,
        List<NotificationContract> notifications)
    {
        var criticalNotifications = notifications.Where(n => n.Severity == NotificationSeverity.Critical).ToList();

        return new NotificationContract
        {
            PrId = pr.PrId,
            PrNumber = pr.PrNumber,
            Type = NotificationType.Alert,
            Severity = NotificationSeverity.Critical,
            Title = "🚨 Critical Issues - PR Review Summary",
            Message = $"PR #{pr.PrNumber} requires immediate attention. {criticalNotifications.Count} critical issue(s) detected.",
            Recipients = new() { "lead-engineer@company.com", "security-team@company.com" },
            Channels = new() { NotificationChannel.Email, NotificationChannel.Teams },
            Data = new()
            {
                { "timestamp", DateTime.UtcNow },
                { "criticalCount", criticalNotifications.Count },
                { "totalNotifications", notifications.Count }
            }
        };
    }
}

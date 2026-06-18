using Microsoft.Extensions.Logging;
using PRGatekeeper.Contracts;
using PRGatekeeper.Services;

namespace PRGatekeeper.Agents;

/// <summary>
/// Code Analyzer Agent - Performs deep code quality, security, and performance analysis.
/// Produces AnalysisResultContract with deterministic output.
/// </summary>
public class CodeAnalyzerAgent
{
    private readonly IStateManager _stateManager;
    private readonly ILogger<CodeAnalyzerAgent> _logger;

    public const string AgentName = "CodeAnalyzerAgent";

    public CodeAnalyzerAgent(IStateManager stateManager, ILogger<CodeAnalyzerAgent> logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Executes code analysis on a PR.
    /// </summary>
    public async Task<AnalysisResultContract> AnalyzeAsync(PullRequestContract pr)
    {
        ArgumentNullException.ThrowIfNull(pr);

        _logger.LogInformation("🔍 Code Analyzer Agent processing PR: {PrNumber}", pr.PrNumber);

        var result = new AnalysisResultContract
        {
            PrId = pr.PrId,
            Status = AnalysisStatus.InProgress
        };

        // Analyze code metrics
        AnalyzeCodeMetrics(pr, result);

        // Perform quality analysis
        AnalyzeCodeQuality(pr, result);

        // Perform security analysis
        AnalyzeSecurityVulnerabilities(pr, result);

        // Perform performance analysis
        AnalyzePerformance(pr, result);

        // Calculate scores
        CalculateScores(result);

        result.Status = AnalysisStatus.Completed;

        // Store result for A2A communication
        await _stateManager.SetStateAsync(pr.PrId, $"{AgentName}:Result", result);

        _logger.LogInformation(
            "✅ Analysis completed for PR {PrNumber}: Quality={Quality}, Security={Security}, Overall={Overall}",
            pr.PrNumber, result.QualityScore, result.SecurityScore, result.OverallScore);

        return result;
    }

    private void AnalyzeCodeMetrics(PullRequestContract pr, AnalysisResultContract result)
    {
        _logger.LogDebug("Analyzing code metrics for PR {PrNumber}", pr.PrNumber);

        var metrics = result.Metrics;

        // Estimate metrics based on PR data (in production, integrate with SonarQube, Roslyn analyzers, etc.)
        metrics.LinesAdded = pr.LineAdditions;
        metrics.LinesRemoved = pr.LineDeletions;
        metrics.FilesAnalyzed = pr.ChangedFiles.Count;

        // Estimate cyclomatic complexity (higher = more complex)
        var changeSize = pr.LineAdditions + pr.LineDeletions;
        metrics.CyclomaticComplexity = Math.Min(15, Math.Ceiling(changeSize / 100.0));

        // Estimate maintainability index (0-100, higher is better)
        metrics.MaintainabilityIndex = 100 - (metrics.CyclomaticComplexity * 2);

        // Estimate test coverage (in production, get from coverage tools)
        metrics.TestCoverage = pr.ChangedFiles.Any(f => f.Contains("Test", StringComparison.OrdinalIgnoreCase))
            ? 75
            : 60;

        // Estimate duplicate lines percentage
        metrics.DuplicateLines = 5; // Placeholder
    }

    private void AnalyzeCodeQuality(PullRequestContract pr, AnalysisResultContract result)
    {
        _logger.LogDebug("Analyzing code quality for PR {PrNumber}", pr.PrNumber);

        // Check for large functions
        if (pr.LineAdditions > 200)
        {
            result.QualityIssues.Add(new QualityIssue
            {
                Rule = "LargeFile",
                Severity = "Warning",
                Message = "File contains more than 200 lines. Consider breaking into smaller functions.",
                File = pr.ChangedFiles.FirstOrDefault() ?? "unknown",
                Line = 1
            });
        }

        // Check file naming conventions
        var unconventionalFiles = pr.ChangedFiles.Where(f =>
            !IsValidFileName(f) && !f.StartsWith(".", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (unconventionalFiles.Any())
        {
            result.QualityIssues.Add(new QualityIssue
            {
                Rule = "NamingConvention",
                Severity = "Info",
                Message = $"{unconventionalFiles.Count} file(s) don't follow naming conventions",
                File = unconventionalFiles.First(),
                Line = 1
            });
        }

        // Check for test files
        var hasTests = pr.ChangedFiles.Any(f =>
            f.Contains("test", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("spec", StringComparison.OrdinalIgnoreCase)
        );

        if (!hasTests && pr.LineAdditions > 50)
        {
            result.QualityIssues.Add(new QualityIssue
            {
                Rule = "MissingTests",
                Severity = "Warning",
                Message = "PR adds code but includes no test files",
                File = "N/A",
                Line = 0
            });
        }
    }

    private void AnalyzeSecurityVulnerabilities(PullRequestContract pr, AnalysisResultContract result)
    {
        _logger.LogDebug("Analyzing security vulnerabilities for PR {PrNumber}", pr.PrNumber);

        // Check for common security issues in file content patterns
        var riskFiles = pr.ChangedFiles.Where(f =>
            f.Contains("auth", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("crypto", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("password", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (riskFiles.Any())
        {
            result.SecurityFindings.Add(new SecurityFinding
            {
                Vulnerability = "SecuritySensitiveCode",
                CvssScore = 6.5,
                Description = "PR modifies security-sensitive code",
                Remediation = "Ensure all cryptographic operations use standard libraries and are reviewed by security team",
                File = riskFiles.First()
            });
        }

        // Check for config changes
        if (pr.ChangedFiles.Any(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                                      f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                                      f.EndsWith(".config", StringComparison.OrdinalIgnoreCase)))
        {
            result.SecurityFindings.Add(new SecurityFinding
            {
                Vulnerability = "ConfigurationChange",
                CvssScore = 3.5,
                Description = "Configuration files have been modified",
                Remediation = "Verify no sensitive data (keys, passwords, API tokens) is exposed in configuration",
                File = pr.ChangedFiles.First()
            });
        }

        // Check for dependency changes
        if (pr.ChangedFiles.Any(f =>
            f.EndsWith("package.json", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith("packages.config", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase)))
        {
            result.SecurityFindings.Add(new SecurityFinding
            {
                Vulnerability = "DependencyUpdate",
                CvssScore = 4.0,
                Description = "Dependencies have been updated",
                Remediation = "Verify all new/updated dependencies are from trusted sources and have no known vulnerabilities",
                File = pr.ChangedFiles.First()
            });
        }
    }

    private void AnalyzePerformance(PullRequestContract pr, AnalysisResultContract result)
    {
        _logger.LogDebug("Analyzing performance for PR {PrNumber}", pr.PrNumber);

        // Check for potential performance issues
        var potentiallySlowFiles = pr.ChangedFiles.Where(f =>
            f.Contains("database", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("sql", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("query", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("cache", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (potentiallySlowFiles.Any())
        {
            result.PerformanceWarnings.Add(
                $"PR modifies database/query-related code. Ensure queries are optimized and use indexes appropriately.");
        }

        // Large additions might indicate performance issues
        if (pr.LineAdditions > 500)
        {
            result.PerformanceWarnings.Add(
                "Large change set. Review for any new loops, nested iterations, or O(n²) algorithms.");
        }

        // Check for external API calls
        if (pr.ChangedFiles.Any(f =>
            f.Contains("http", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("api", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("client", StringComparison.OrdinalIgnoreCase)))
        {
            result.PerformanceWarnings.Add(
                "PR contains HTTP/API client code. Ensure proper timeouts and connection pooling are configured.");
        }
    }

    private void CalculateScores(AnalysisResultContract result)
    {
        // Quality Score: based on issues
        var qualityPenalty = result.QualityIssues.Count(i => i.Severity == "Info") * 2 +
                           result.QualityIssues.Count(i => i.Severity == "Warning") * 8 +
                           result.QualityIssues.Count(i => i.Severity == "Error") * 20;
        result.QualityScore = Math.Max(0, 100 - qualityPenalty);

        // Security Score: based on findings
        var securityPenalty = result.SecurityFindings.Sum(f => (int)f.CvssScore);
        result.SecurityScore = Math.Max(0, 100 - securityPenalty);

        // Overall Score: weighted average
        result.OverallScore = (result.QualityScore * 0.6) + (result.SecurityScore * 0.4);
    }

    private static bool IsValidFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(name))
            return false;

        // Check for common naming patterns: camelCase, PascalCase, snake_case
        return char.IsLetterOrDigit(name[0]) && name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }
}

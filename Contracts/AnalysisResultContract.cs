using System.Text.Json.Serialization;

namespace PRGatekeeper.Contracts;

/// <summary>
/// Contract for code analysis results produced by the Code Analyzer Agent.
/// </summary>
public class AnalysisResultContract
{
    [JsonPropertyName("analysisId")]
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("prId")]
    public string PrId { get; set; } = string.Empty;

    [JsonPropertyName("agentName")]
    public string AgentName { get; set; } = "CodeAnalyzerAgent";

    [JsonPropertyName("status")]
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;

    [JsonPropertyName("codeMetrics")]
    public CodeMetrics Metrics { get; set; } = new();

    [JsonPropertyName("qualityIssues")]
    public List<QualityIssue> QualityIssues { get; set; } = new();

    [JsonPropertyName("securityFindings")]
    public List<SecurityFinding> SecurityFindings { get; set; } = new();

    [JsonPropertyName("performanceWarnings")]
    public List<string> PerformanceWarnings { get; set; } = new();

    [JsonPropertyName("qualityScore")]
    public double QualityScore { get; set; }

    [JsonPropertyName("securityScore")]
    public double SecurityScore { get; set; }

    [JsonPropertyName("overallScore")]
    public double OverallScore { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; set; }

    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; set; } = "1.0";
}

public enum AnalysisStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public class CodeMetrics
{
    [JsonPropertyName("cyclomatic")]
    public double CyclomaticComplexity { get; set; }

    [JsonPropertyName("maintainability")]
    public double MaintainabilityIndex { get; set; }

    [JsonPropertyName("coverage")]
    public double TestCoverage { get; set; }

    [JsonPropertyName("duplicates")]
    public double DuplicateLines { get; set; }

    [JsonPropertyName("linesAdded")]
    public int LinesAdded { get; set; }

    [JsonPropertyName("linesRemoved")]
    public int LinesRemoved { get; set; }

    [JsonPropertyName("filesAnalyzed")]
    public int FilesAnalyzed { get; set; }
}

public class QualityIssue
{
    [JsonPropertyName("issueId")]
    public string IssueId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("rule")]
    public string Rule { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "Info";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int Line { get; set; }
}

public class SecurityFinding
{
    [JsonPropertyName("findingId")]
    public string FindingId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("vulnerability")]
    public string Vulnerability { get; set; } = string.Empty;

    [JsonPropertyName("cvss")]
    public double CvssScore { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("remediation")]
    public string Remediation { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
}

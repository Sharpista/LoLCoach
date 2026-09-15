namespace LoLCoach.Api.Analytics.Insights;

public sealed record Insight(
    InsightType Type,
    InsightSeverity Severity,
    string Metric,
    decimal CurrentValue,
    decimal TargetValue,
    string Evidence,
    int MatchesAnalyzed);

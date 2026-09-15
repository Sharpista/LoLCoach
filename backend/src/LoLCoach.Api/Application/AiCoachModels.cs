namespace LoLCoach.Api.Application;

public sealed record CoachAnalysisInput(
    PerformanceSummaryDto Summary,
    IReadOnlyList<AnalysisInsightDto> Insights,
    IReadOnlyList<RecommendationDto> Recommendations);

public sealed record CoachReport(
    string PeriodSummary,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    string MainPriority,
    IReadOnlyList<CoachGoalDto> Plan,
    bool GeneratedByAi);

public sealed record CoachGoalDto(string GoalMetric, decimal TargetValue, string Action);

using LoLCoach.Api.Analytics.Insights;

namespace LoLCoach.Api.Analytics.Recommendations;

public sealed record Recommendation(
    InsightType ProblemType,
    string Evidence,
    string ImpactContext,
    string RecommendationText,
    string GoalMetric,
    decimal TargetValue);

using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Recommendations;

namespace LoLCoach.Tests;

public sealed class RecommendationEngineTests
{
    [Theory]
    [InlineData(InsightType.LowCs, "csPerMinute", 6.5, "LOW_CS")]
    [InlineData(InsightType.HighDeaths, "averageDeaths", 5.0, "HIGH_DEATHS")]
    [InlineData(InsightType.LowVision, "visionPerMinute", 0.8, "LOW_VISION")]
    [InlineData(InsightType.LowCombatImpact, "kda", 2.5, "LOW_COMBAT_IMPACT")]
    [InlineData(InsightType.InconsistentPerformance, "kdaStandardDeviation", 2.0, "INCONSISTENT_PERFORMANCE")]
    public void Generate_maps_supported_insights_to_measurable_recommendations(
        InsightType type,
        string metric,
        double target,
        string problemType)
    {
        var insight = new Insight(type, InsightSeverity.High, metric, 1.2m, (decimal)target,
            "evidência calculada", 12);

        var recommendation = Assert.Single(new RecommendationEngine().Generate([insight]));

        Assert.Equal(type, recommendation.ProblemType);
        Assert.Equal("evidência calculada", recommendation.Evidence);
        Assert.Contains(problemType, recommendation.ImpactContext);
        Assert.False(string.IsNullOrWhiteSpace(recommendation.RecommendationText));
        Assert.Equal(metric, recommendation.GoalMetric);
        Assert.Equal((decimal)target, recommendation.TargetValue);
    }

    [Fact]
    public void Generate_ignores_unsupported_champion_insights()
    {
        var insight = new Insight(InsightType.BestChampion, InsightSeverity.Low, "championWinrate", 80, 50,
            "melhor campeão", 10);

        Assert.Empty(new RecommendationEngine().Generate([insight]));
    }
}

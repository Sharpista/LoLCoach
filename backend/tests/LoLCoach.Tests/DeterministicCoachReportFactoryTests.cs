using LoLCoach.Api.Application;

namespace LoLCoach.Tests;

public sealed class DeterministicCoachReportFactoryTests
{
    [Fact]
    public void Create_uses_only_structured_summary_and_recommendations()
    {
        var input = new CoachAnalysisInput(
            new PerformanceSummaryDto(5, 60, 2.7m, 6.8m, 0.9m, 450),
            [new AnalysisInsightDto("low_cs", "high", "Farm baixo", "CS/min médio de 5.2", "LOW_CS", "csPerMinute", 5.2m, 6.5m, 5)],
            [new RecommendationDto("LOW_CS", "CS/min médio de 5.2", "Prioridade alta", "Treinar last hit", "csPerMinute", 6.5m)]);

        var report = DeterministicCoachReportFactory.Create(input);

        Assert.False(report.GeneratedByAi);
        Assert.Contains("5 partidas", report.PeriodSummary);
        Assert.Contains(report.Strengths, strength => strength.Contains("60%", StringComparison.Ordinal));
        var goal = Assert.Single(report.Plan);
        Assert.Equal("csPerMinute", goal.GoalMetric);
        Assert.Equal(6.5m, goal.TargetValue);
        Assert.Equal("Treinar last hit", goal.Action);
    }
}

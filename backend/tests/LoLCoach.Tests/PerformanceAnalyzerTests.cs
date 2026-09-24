using LoLCoach.Api.Analytics.Analyzers;
using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Tests;

public sealed class PerformanceAnalyzerTests
{
    [Fact]
    public void Farming_analyzer_emits_measurable_low_cs_insight()
    {
        var metrics = Metrics(csPerMinute: 4.8m);

        var insight = Assert.Single(new FarmingAnalyzer().Analyze(metrics));

        Assert.Equal(InsightType.LowCs, insight.Type);
        Assert.Equal(InsightSeverity.High, insight.Severity);
        Assert.Equal("csPerMinute", insight.Metric);
        Assert.Equal(4.8m, insight.CurrentValue);
        Assert.Equal(6.5m, insight.TargetValue);
        Assert.Equal(20, insight.MatchesAnalyzed);
    }

    [Fact]
    public void Death_analyzer_emits_high_deaths_insight()
    {
        var insight = Assert.Single(new DeathAnalyzer().Analyze(Metrics(averageDeaths: 7.1m)));

        Assert.Equal(InsightType.HighDeaths, insight.Type);
        Assert.Equal(InsightSeverity.High, insight.Severity);
        Assert.Equal("averageDeaths", insight.Metric);
    }

    [Fact]
    public void Vision_analyzer_emits_low_vision_insight()
    {
        var insight = Assert.Single(new VisionAnalyzer().Analyze(Metrics(visionPerMinute: 0.4m)));

        Assert.Equal(InsightType.LowVision, insight.Type);
        Assert.Equal(InsightSeverity.High, insight.Severity);
        Assert.Equal("visionPerMinute", insight.Metric);
    }

    [Fact]
    public void Combat_analyzer_emits_low_combat_impact_insight()
    {
        var insight = Assert.Single(new CombatAnalyzer().Analyze(Metrics(kda: 1.4m)));

        Assert.Equal(InsightType.LowCombatImpact, insight.Type);
        Assert.Equal(InsightSeverity.High, insight.Severity);
        Assert.Equal("kda", insight.Metric);
    }

    [Fact]
    public void Consistency_analyzer_requires_enough_matches_and_variation()
    {
        var insight = Assert.Single(new ConsistencyAnalyzer().Analyze(Metrics(kdaStandardDeviation: 3.2m)));

        Assert.Equal(InsightType.InconsistentPerformance, insight.Type);
        Assert.Equal(InsightSeverity.High, insight.Severity);
        Assert.Empty(new ConsistencyAnalyzer().Analyze(Metrics(matches: 2, kdaStandardDeviation: 5m)));
    }

    [Fact]
    public void Champion_analyzer_reports_best_and_worst_champions_deterministically()
    {
        var metrics = Metrics(champions:
        [
            new ChampionMetrics("Ahri", 5, 4, 80, 4.2m, 7.1m, 0.8m, 600),
            new ChampionMetrics("Lux", 4, 1, 25, 1.8m, 5.5m, 0.7m, 450),
        ]);

        var insights = new ChampionAnalyzer().Analyze(metrics).ToList();

        Assert.Collection(insights,
            insight => Assert.Equal(InsightType.BestChampion, insight.Type),
            insight => Assert.Equal(InsightType.WorstChampion, insight.Type));
    }

    private static PlayerMetrics Metrics(
        int matches = 20,
        decimal averageDeaths = 5,
        decimal kda = 3,
        decimal csPerMinute = 7,
        decimal visionPerMinute = 1,
        decimal kdaStandardDeviation = 1,
        IReadOnlyList<ChampionMetrics>? champions = null)
        => new(
            matches,
            10,
            10,
            50,
            5,
            averageDeaths,
            8,
            kda,
            180,
            csPerMinute,
            20,
            visionPerMinute,
            15_000,
            500,
            kdaStandardDeviation,
            champions ?? [],
            []);
}

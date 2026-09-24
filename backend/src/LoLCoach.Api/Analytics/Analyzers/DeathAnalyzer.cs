using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public sealed class DeathAnalyzer : IPerformanceAnalyzer
{
    private const decimal TargetAverageDeaths = 5.0m;
    private const decimal HighSeverityThreshold = 7.0m;

    public IEnumerable<Insight> Analyze(PlayerMetrics metrics)
    {
        if (metrics.Matches == 0 || metrics.AverageDeaths <= TargetAverageDeaths)
            yield break;

        yield return new Insight(
            InsightType.HighDeaths,
            metrics.AverageDeaths > HighSeverityThreshold ? InsightSeverity.High : InsightSeverity.Medium,
            "averageDeaths",
            metrics.AverageDeaths,
            TargetAverageDeaths,
            $"Média de {metrics.AverageDeaths} mortes em {metrics.Matches} partidas; alvo inicial até {TargetAverageDeaths}.",
            metrics.Matches);
    }
}

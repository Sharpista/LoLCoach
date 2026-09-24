using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public sealed class ConsistencyAnalyzer : IPerformanceAnalyzer
{
    private const decimal TargetKdaDeviation = 2.0m;
    private const decimal HighSeverityThreshold = 3.0m;

    public IEnumerable<Insight> Analyze(PlayerMetrics metrics)
    {
        if (metrics.Matches < 3 || metrics.KdaStandardDeviation <= TargetKdaDeviation)
            yield break;

        yield return new Insight(
            InsightType.InconsistentPerformance,
            metrics.KdaStandardDeviation > HighSeverityThreshold ? InsightSeverity.High : InsightSeverity.Medium,
            "kdaStandardDeviation",
            metrics.KdaStandardDeviation,
            TargetKdaDeviation,
            $"Desvio padrão de KDA de {metrics.KdaStandardDeviation} em {metrics.Matches} partidas; alvo inicial até {TargetKdaDeviation}.",
            metrics.Matches);
    }
}

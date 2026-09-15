using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public sealed class CombatAnalyzer : IPerformanceAnalyzer
{
    private const decimal TargetKda = 2.5m;
    private const decimal HighSeverityThreshold = 1.5m;

    public IEnumerable<Insight> Analyze(PlayerMetrics metrics)
    {
        if (metrics.Matches == 0 || metrics.Kda >= TargetKda)
            yield break;

        yield return new Insight(
            InsightType.LowCombatImpact,
            metrics.Kda < HighSeverityThreshold ? InsightSeverity.High : InsightSeverity.Medium,
            "kda",
            metrics.Kda,
            TargetKda,
            $"KDA médio de {metrics.Kda} em {metrics.Matches} partidas; alvo inicial {TargetKda}.",
            metrics.Matches);
    }
}

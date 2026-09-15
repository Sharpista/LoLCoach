using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public sealed class FarmingAnalyzer : IPerformanceAnalyzer
{
    private const decimal TargetCsPerMinute = 6.5m;
    private const decimal HighSeverityThreshold = 5.0m;

    public IEnumerable<Insight> Analyze(PlayerMetrics metrics)
    {
        if (metrics.Matches == 0 || metrics.CsPerMinute >= TargetCsPerMinute)
            yield break;

        yield return new Insight(
            InsightType.LowCs,
            metrics.CsPerMinute < HighSeverityThreshold ? InsightSeverity.High : InsightSeverity.Medium,
            "csPerMinute",
            metrics.CsPerMinute,
            TargetCsPerMinute,
            $"CS/min médio de {metrics.CsPerMinute} em {metrics.Matches} partidas; alvo inicial {TargetCsPerMinute}.",
            metrics.Matches);
    }
}

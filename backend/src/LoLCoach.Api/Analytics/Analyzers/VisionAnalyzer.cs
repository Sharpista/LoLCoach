using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public sealed class VisionAnalyzer : IPerformanceAnalyzer
{
    private const decimal TargetVisionPerMinute = 0.8m;
    private const decimal HighSeverityThreshold = 0.5m;

    public IEnumerable<Insight> Analyze(PlayerMetrics metrics)
    {
        if (metrics.Matches == 0 || metrics.VisionPerMinute >= TargetVisionPerMinute)
            yield break;

        yield return new Insight(
            InsightType.LowVision,
            metrics.VisionPerMinute < HighSeverityThreshold ? InsightSeverity.High : InsightSeverity.Medium,
            "visionPerMinute",
            metrics.VisionPerMinute,
            TargetVisionPerMinute,
            $"Vision/min médio de {metrics.VisionPerMinute} em {metrics.Matches} partidas; alvo inicial {TargetVisionPerMinute}.",
            metrics.Matches);
    }
}

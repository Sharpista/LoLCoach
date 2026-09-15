using LoLCoach.Api.Analytics.Analyzers;
using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Application;

public sealed class PerformanceAnalysisService(
    IPlayerRepository playerRepository,
    IMatchRepository matchRepository,
    PlayerMetricsCalculator metricsCalculator,
    IEnumerable<IPerformanceAnalyzer> analyzers)
{
    private const int InsightLimit = 3;

    public async Task<PerformanceAnalysisDto> AnalyzeAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var player = await playerRepository.FindByIdAsync(playerId, cancellationToken)
            ?? throw new PlayerNotFoundException(playerId);
        var playerMatches = await matchRepository.ListPlayerMatchesForAnalysisAsync(playerId, cancellationToken);
        var metrics = metricsCalculator.Calculate(playerMatches);
        var insights = analyzers
            .SelectMany(analyzer => analyzer.Analyze(metrics))
            .GroupBy(insight => insight.Type)
            .Select(group => group.OrderBy(SeverityRank).ThenByDescending(InsightGap).First())
            .OrderBy(SeverityRank)
            .ThenByDescending(InsightGap)
            .ThenBy(insight => insight.Type.ToString(), StringComparer.Ordinal)
            .Take(InsightLimit)
            .Select(ToDto)
            .ToList();

        return new PerformanceAnalysisDto(
            new AnalysisPlayerDto(player.Id, player.GameName, player.TagLine, player.Region),
            new PerformanceSummaryDto(
                metrics.Matches,
                metrics.WinRate,
                metrics.Kda,
                metrics.CsPerMinute,
                metrics.VisionPerMinute,
                metrics.DamagePerMinute),
            insights,
            metrics.Champions.Select(champion => new ChampionPerformanceDto(
                champion.Champion,
                champion.Games,
                champion.WinRate,
                champion.Kda)).ToList(),
            metrics.RecentMatches.Select(match => new RecentMatchDto(
                match.Id,
                match.Champion,
                match.Win ? "win" : "loss",
                match.KdaText,
                match.Cs,
                match.DurationMinutes,
                match.PlayedAt)).ToList());
    }

    private static AnalysisInsightDto ToDto(Insight insight) => new(
        ToStableId(insight.Type),
        ToCamelCase(insight.Severity.ToString()),
        ToTitle(insight.Type),
        insight.Evidence,
        ToInsightTypeCode(insight.Type),
        insight.Metric,
        insight.CurrentValue,
        insight.TargetValue,
        insight.MatchesAnalyzed);

    private static int SeverityRank(Insight insight) => insight.Severity switch
    {
        InsightSeverity.High => 0,
        InsightSeverity.Medium => 1,
        InsightSeverity.Low => 2,
        _ => 3,
    };

    private static decimal InsightGap(Insight insight)
        => insight.TargetValue == 0 ? 0 : Math.Abs((insight.CurrentValue - insight.TargetValue) / insight.TargetValue);

    private static string ToStableId(InsightType type) => ToInsightTypeCode(type).ToLowerInvariant();

    private static string ToInsightTypeCode(InsightType type) => type switch
    {
        InsightType.LowCs => "LOW_CS",
        InsightType.HighDeaths => "HIGH_DEATHS",
        InsightType.LowVision => "LOW_VISION",
        InsightType.LowCombatImpact => "LOW_COMBAT_IMPACT",
        InsightType.InconsistentPerformance => "INCONSISTENT_PERFORMANCE",
        InsightType.BestChampion => "BEST_CHAMPION",
        InsightType.WorstChampion => "WORST_CHAMPION",
        _ => type.ToString().ToUpperInvariant(),
    };

    private static string ToTitle(InsightType type) => type switch
    {
        InsightType.LowCs => "Farm baixo",
        InsightType.HighDeaths => "Mortes excessivas",
        InsightType.LowVision => "Visão baixa",
        InsightType.LowCombatImpact => "KDA baixo",
        InsightType.InconsistentPerformance => "Performance inconsistente",
        InsightType.BestChampion => "Melhor campeão",
        InsightType.WorstChampion => "Campeão em atenção",
        _ => type.ToString(),
    };

    private static string ToCamelCase(string value) => string.Concat(value[..1].ToLowerInvariant(), value.AsSpan(1));
}

using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;

namespace LoLCoach.Api.Analytics.Analyzers;

public sealed class ChampionAnalyzer : IPerformanceAnalyzer
{
    public IEnumerable<Insight> Analyze(PlayerMetrics metrics)
    {
        if (metrics.Champions.Count == 0)
            yield break;

        var best = metrics.Champions
            .OrderByDescending(champion => champion.WinRate)
            .ThenByDescending(champion => champion.Kda)
            .ThenByDescending(champion => champion.Games)
            .ThenBy(champion => champion.Champion, StringComparer.Ordinal)
            .First();

        yield return new Insight(
            InsightType.BestChampion,
            InsightSeverity.Low,
            "championWinRate",
            best.WinRate,
            50,
            $"Melhor campeão recente: {best.Champion} com {best.WinRate}% de winrate, KDA {best.Kda}, em {best.Games} partida(s).",
            best.Games);

        if (metrics.Champions.Count < 2)
            yield break;

        var worst = metrics.Champions
            .OrderBy(champion => champion.WinRate)
            .ThenBy(champion => champion.Kda)
            .ThenByDescending(champion => champion.Games)
            .ThenBy(champion => champion.Champion, StringComparer.Ordinal)
            .First();

        yield return new Insight(
            InsightType.WorstChampion,
            InsightSeverity.Medium,
            "championWinRate",
            worst.WinRate,
            50,
            $"Campeão com pior recorte recente: {worst.Champion} com {worst.WinRate}% de winrate, KDA {worst.Kda}, em {worst.Games} partida(s).",
            worst.Games);
    }
}

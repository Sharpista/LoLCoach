using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Analytics.Metrics;

public sealed class PlayerMetricsCalculator
{
    public PlayerMetrics Calculate(IEnumerable<PlayerMatch> matches)
    {
        var orderedMatches = matches
            .OrderByDescending(match => match.Match.GameStart)
            .ThenBy(match => match.Id)
            .ToList();

        if (orderedMatches.Count == 0)
        {
            return new PlayerMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, [], []);
        }

        var totalMinutes = orderedMatches.Sum(DurationMinutes);
        var wins = orderedMatches.Count(match => match.Win);
        var perMatchKdas = orderedMatches.Select(CalculateKda).ToList();
        var kda = Average(perMatchKdas);

        var champions = orderedMatches
            .GroupBy(match => match.ChampionName)
            .Select(group => ToChampionMetrics(group.Key, group.ToList()))
            .OrderByDescending(champion => champion.Games)
            .ThenByDescending(champion => champion.WinRate)
            .ThenBy(champion => champion.Champion, StringComparer.Ordinal)
            .ToList();

        var recentMatches = orderedMatches
            .Take(10)
            .Select(match => new RecentMatchMetrics(
                match.Match.Id,
                match.ChampionName,
                match.Win,
                match.Kills,
                match.Deaths,
                match.Assists,
                match.TotalCs,
                Round(DurationMinutes(match)),
                match.Match.GameStart))
            .ToList();

        return new PlayerMetrics(
            orderedMatches.Count,
            wins,
            orderedMatches.Count - wins,
            Round(Percent(wins, orderedMatches.Count)),
            Round(Average(orderedMatches.Select(match => (decimal)match.Kills))),
            Round(Average(orderedMatches.Select(match => (decimal)match.Deaths))),
            Round(Average(orderedMatches.Select(match => (decimal)match.Assists))),
            Round(kda),
            Round(Average(orderedMatches.Select(match => (decimal)match.TotalCs))),
            Round(SafeDivide(orderedMatches.Sum(match => match.TotalCs), totalMinutes)),
            Round(Average(orderedMatches.Select(match => (decimal)match.VisionScore))),
            Round(SafeDivide(orderedMatches.Sum(match => match.VisionScore), totalMinutes)),
            Round(Average(orderedMatches.Select(match => (decimal)match.DamageToChampions))),
            Round(SafeDivide(orderedMatches.Sum(match => match.DamageToChampions), totalMinutes)),
            Round(StandardDeviation(perMatchKdas)),
            champions,
            recentMatches);
    }

    private static ChampionMetrics ToChampionMetrics(string champion, IReadOnlyCollection<PlayerMatch> matches)
    {
        var totalMinutes = matches.Sum(DurationMinutes);
        var wins = matches.Count(match => match.Win);
        return new ChampionMetrics(
            champion,
            matches.Count,
            wins,
            Round(Percent(wins, matches.Count)),
            Round(Average(matches.Select(CalculateKda))),
            Round(SafeDivide(matches.Sum(match => match.TotalCs), totalMinutes)),
            Round(SafeDivide(matches.Sum(match => match.VisionScore), totalMinutes)),
            Round(SafeDivide(matches.Sum(match => match.DamageToChampions), totalMinutes)));
    }

    private static decimal CalculateKda(PlayerMatch match)
        => match.Deaths == 0 ? match.Kills + match.Assists : SafeDivide(match.Kills + match.Assists, match.Deaths);

    private static decimal DurationMinutes(PlayerMatch match) => Math.Max(1, match.Match.GameDuration) / 60m;

    private static decimal Average(IEnumerable<decimal> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? 0 : list.Sum() / list.Count;
    }

    private static decimal Percent(int value, int total) => total == 0 ? 0 : SafeDivide(value * 100m, total);

    private static decimal SafeDivide(decimal numerator, decimal denominator) => denominator == 0 ? 0 : numerator / denominator;

    private static decimal StandardDeviation(IReadOnlyList<decimal> values)
    {
        if (values.Count <= 1)
            return 0;

        var average = values.Sum() / values.Count;
        var variance = values.Sum(value => (value - average) * (value - average)) / values.Count;
        return (decimal)Math.Sqrt((double)variance);
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

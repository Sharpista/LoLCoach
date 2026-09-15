using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Analytics.Metrics;

public sealed record PlayerMetrics(
    int Matches,
    int Wins,
    int Losses,
    decimal WinRate,
    decimal AverageKills,
    decimal AverageDeaths,
    decimal AverageAssists,
    decimal Kda,
    decimal AverageCs,
    decimal CsPerMinute,
    decimal AverageVisionScore,
    decimal VisionPerMinute,
    decimal AverageDamage,
    decimal DamagePerMinute,
    decimal KdaStandardDeviation,
    IReadOnlyList<ChampionMetrics> Champions,
    IReadOnlyList<RecentMatchMetrics> RecentMatches);

public sealed record ChampionMetrics(
    string Champion,
    int Games,
    int Wins,
    decimal WinRate,
    decimal Kda,
    decimal CsPerMinute,
    decimal VisionPerMinute,
    decimal DamagePerMinute);

public sealed record RecentMatchMetrics(
    Guid Id,
    string Champion,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int Cs,
    decimal DurationMinutes,
    DateTimeOffset PlayedAt)
{
    public string KdaText => $"{Kills}/{Deaths}/{Assists}";
}

using LoLCoach.Api.Analytics.Analyzers;
using LoLCoach.Api.Analytics.Insights;
using LoLCoach.Api.Analytics.Metrics;
using LoLCoach.Api.Domain;

namespace LoLCoach.Tests;

public sealed class PlayerMetricsCalculatorTests
{
    [Fact]
    public void Same_matches_produce_same_metrics_and_calculate_rates()
    {
        var matches = new[]
        {
            CreatePlayerMatch("BR1_1", "Ahri", true, 10, 2, 8, 210, 18_000, 24, 1800,
                DateTimeOffset.Parse("2026-09-01T00:00:00Z")),
            CreatePlayerMatch("BR1_2", "Ahri", false, 4, 4, 6, 170, 12_000, 15, 1500,
                DateTimeOffset.Parse("2026-09-02T00:00:00Z")),
        };
        var calculator = new PlayerMetricsCalculator();

        var first = calculator.Calculate(matches);
        var second = calculator.Calculate(matches.Reverse());

        Assert.Equal(first.Matches, second.Matches);
        Assert.Equal(first.WinRate, second.WinRate);
        Assert.Equal(first.Kda, second.Kda);
        Assert.Equal(first.CsPerMinute, second.CsPerMinute);
        Assert.Equal(first.VisionPerMinute, second.VisionPerMinute);
        Assert.Equal(first.DamagePerMinute, second.DamagePerMinute);
        Assert.Equal(first.Champions, second.Champions);
        Assert.Equal(first.RecentMatches.Select(match => match.Id), second.RecentMatches.Select(match => match.Id));
        Assert.Equal(2, first.Matches);
        Assert.Equal(1, first.Wins);
        Assert.Equal(1, first.Losses);
        Assert.Equal(50, first.WinRate);
        Assert.Equal(5.75m, first.Kda);
        Assert.Equal(6.91m, first.CsPerMinute);
        Assert.Equal(0.71m, first.VisionPerMinute);
        Assert.Equal(545.45m, first.DamagePerMinute);
        var champion = Assert.Single(first.Champions);
        Assert.Equal("Ahri", champion.Champion);
        Assert.Equal(2, champion.Games);
    }

    [Fact]
    public void Empty_collection_returns_zero_metrics()
    {
        var metrics = new PlayerMetricsCalculator().Calculate([]);

        Assert.Equal(0, metrics.Matches);
        Assert.Equal(0, metrics.WinRate);
        Assert.Empty(metrics.Champions);
        Assert.Empty(metrics.RecentMatches);
    }

    public static PlayerMatch CreatePlayerMatch(string riotMatchId, string champion, bool win, int kills, int deaths,
        int assists, int cs, int damage, int vision, int durationSeconds, DateTimeOffset playedAt)
    {
        var match = new Match(Guid.NewGuid(), riotMatchId, playedAt, durationSeconds, 420, "CLASSIC");
        var playerMatch = new PlayerMatch(Guid.NewGuid(), Guid.NewGuid(), 1, champion, "MID", win, kills, deaths,
            assists, cs, 10_000, damage, 15_000, vision, 10, 3);
        match.AddPlayerMatch(playerMatch);
        return playerMatch;
    }
}

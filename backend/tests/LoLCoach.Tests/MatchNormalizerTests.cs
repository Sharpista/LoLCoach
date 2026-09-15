using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;

namespace LoLCoach.Tests;

public sealed class MatchNormalizerTests
{
    private static readonly Player Player = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "target-puuid",
        "Example",
        "TAG",
        "br1",
        DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    [Fact]
    public void Normalize_maps_match_and_participant_fields_by_puuid()
    {
        var details = CreateDetails([
            new RiotMatchParticipant("other-puuid", 1, "Annie", "MIDDLE", false, 1, 2, 3, 100, 4, 5000, 6000, 7000, 8, 9, 10),
            new RiotMatchParticipant("target-puuid", 64, "LeeSin", "JUNGLE", true, 11, 2, 13, 20, 140, 12000, 33000, 21000, 29, 12, 4),
        ]);

        var result = new MatchNormalizer().Normalize(details, Player);

        Assert.NotNull(result);
        Assert.Equal("BR1_123", result.Match.RiotMatchId);
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1780000000000), result.Match.GameStart);
        Assert.Equal(1800, result.Match.GameDuration);
        Assert.Equal(420, result.Match.QueueId);
        Assert.Equal("CLASSIC", result.Match.GameMode);
        Assert.Equal(Player.Id, result.PlayerMatch.PlayerId);
        Assert.Equal(64, result.PlayerMatch.ChampionId);
        Assert.Equal("LeeSin", result.PlayerMatch.ChampionName);
        Assert.Equal("JUNGLE", result.PlayerMatch.TeamPosition);
        Assert.True(result.PlayerMatch.Win);
        Assert.Equal(11, result.PlayerMatch.Kills);
        Assert.Equal(2, result.PlayerMatch.Deaths);
        Assert.Equal(13, result.PlayerMatch.Assists);
        Assert.Equal(160, result.PlayerMatch.TotalCs);
        Assert.Equal(12000, result.PlayerMatch.GoldEarned);
        Assert.Equal(33000, result.PlayerMatch.DamageToChampions);
        Assert.Equal(21000, result.PlayerMatch.DamageTaken);
        Assert.Equal(29, result.PlayerMatch.VisionScore);
        Assert.Equal(12, result.PlayerMatch.WardsPlaced);
        Assert.Equal(4, result.PlayerMatch.WardsKilled);
        Assert.Same(result.PlayerMatch, Assert.Single(result.Match.PlayerMatches));
    }

    [Fact]
    public void Normalize_returns_null_when_player_puuid_is_absent()
    {
        var details = CreateDetails([
            new RiotMatchParticipant("other-puuid", 1, "Annie", "MIDDLE", false, 1, 2, 3, 100, 4, 5000, 6000, 7000, 8, 9, 10),
        ]);

        Assert.Null(new MatchNormalizer().Normalize(details, Player));
    }

    private static RiotMatchDetails CreateDetails(IReadOnlyList<RiotMatchParticipant> participants) => new(
        new RiotMatchMetadata("BR1_123", participants.Select(participant => participant.Puuid).ToArray()),
        new RiotMatchInfo(1779999999000, 1780000000000, 1800, 420, "CLASSIC", participants));
}

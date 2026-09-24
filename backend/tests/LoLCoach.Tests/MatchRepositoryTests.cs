using LoLCoach.Api.Domain;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LoLCoach.Tests;

public sealed class MatchRepositoryTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task New_match_roundtrips_player_match_fields_in_PostgreSQL()
    {
        var player = await SavePlayerAsync();
        var match = CreateMatch("BR1_roundtrip", player.Id);
        await using var db = postgres.CreateContext();
        var repository = new MatchRepository(db);

        await repository.AddAsync(match);
        await repository.SaveChangesAsync();

        Assert.True(await repository.ExistsByRiotMatchIdAsync("BR1_roundtrip"));
        var loaded = await db.Matches.Include(value => value.PlayerMatches)
            .SingleAsync(value => value.RiotMatchId == "BR1_roundtrip");
        var playerMatch = Assert.Single(loaded.PlayerMatches);
        Assert.Equal(player.Id, playerMatch.PlayerId);
        Assert.Equal(64, playerMatch.ChampionId);
        Assert.Equal("LeeSin", playerMatch.ChampionName);
        Assert.Equal("JUNGLE", playerMatch.TeamPosition);
        Assert.True(playerMatch.Win);
        Assert.Equal(160, playerMatch.TotalCs);
        Assert.Equal(33000, playerMatch.DamageToChampions);
    }

    [Fact]
    public async Task Database_itself_rejects_duplicate_riot_match_id()
    {
        var firstPlayer = await SavePlayerAsync();
        var secondPlayer = await SavePlayerAsync();
        await using var db = postgres.CreateContext();
        db.Matches.Add(CreateMatch("BR1_duplicate", firstPlayer.Id));
        await db.SaveChangesAsync();
        db.Matches.Add(CreateMatch("BR1_duplicate", secondPlayer.Id));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresError.SqlState);
        Assert.Equal("ix_matches_riot_match_id", postgresError.ConstraintName);
    }

    private async Task<Player> SavePlayerAsync()
    {
        await using var db = postgres.CreateContext();
        var player = new Player(Guid.NewGuid(), Guid.NewGuid().ToString("N"), "Example", "TAG", "br1",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        return await new PlayerRepository(db).SaveAsync(player);
    }

    private static Match CreateMatch(string riotMatchId, Guid playerId)
    {
        var match = new Match(Guid.NewGuid(), riotMatchId, DateTimeOffset.Parse("2026-02-01T00:00:00Z"), 1800,
            420, "CLASSIC");
        match.AddPlayerMatch(new PlayerMatch(Guid.NewGuid(), playerId, 64, "LeeSin", "JUNGLE", true, 11, 2, 13,
            160, 12000, 33000, 21000, 29, 12, 4));
        return match;
    }
}

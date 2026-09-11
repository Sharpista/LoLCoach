using LoLCoach.Api.Domain;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LoLCoach.Tests;

public sealed class PlayerRepositoryTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static Player Create(string? puuid = null, string name = "Example") => new(
        Guid.NewGuid(), puuid ?? Guid.NewGuid().ToString("N"), name, "TAG", "opaque-region",
        DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    [Fact]
    public async Task Unknown_puuid_returns_null()
    {
        await using var db = postgres.CreateContext();
        Assert.Null(await new PlayerRepository(db).FindByPuuidAsync(Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public async Task Database_itself_rejects_duplicate_puuid()
    {
        var player = Create();
        await using var db = postgres.CreateContext();
        await new PlayerRepository(db).SaveAsync(player);
        db.Players.Add(Create(player.Puuid));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        var postgresError = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresError.SqlState);
        Assert.Equal("ix_players_puuid", postgresError.ConstraintName);
    }

    [Fact]
    public async Task Concurrent_search_saves_have_one_row_and_one_identity()
    {
        var puuid = Guid.NewGuid().ToString("N");
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(async index =>
        {
            await using var db = postgres.CreateContext();
            return await new PlayerRepository(db).SaveAsync(Create(puuid, $"Name{index}"));
        }));
        Assert.Single(results.Select(player => player.Id).Distinct());
        await using var verify = postgres.CreateContext();
        Assert.Equal(1, await verify.Players.CountAsync(player => player.Puuid == puuid));
    }

    [Fact]
    public async Task Sql_metacharacters_remain_literal_data()
    {
        var input = Create(name: "'; DROP TABLE players; --");
        await using var db = postgres.CreateContext();
        var repository = new PlayerRepository(db);
        await repository.SaveAsync(input);
        Assert.Equal(input.GameName, (await repository.FindByPuuidAsync(input.Puuid))!.GameName);
    }

    [Fact]
    public async Task Initial_migration_is_applied_and_model_has_no_pending_changes()
    {
        await using var db = postgres.CreateContext();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Single(applied);
        Assert.EndsWith("_InitialPlayers", applied.Single());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        await using var db = postgres.CreateContext();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new PlayerRepository(db);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveAsync(Create(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.FindByPuuidAsync("absent", cancellation.Token));
    }

    [Fact]
    public async Task Existing_puuid_updates_without_replacing_identity_or_created_date()
    {
        await using var db = postgres.CreateContext();
        var repository = new PlayerRepository(db);
        var original = Create();
        await repository.SaveAsync(original);
        var later = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
        var update = new Player(Guid.NewGuid(), original.Puuid, "Renamed", "NEW", "other-region", later, later);
        var saved = await repository.SaveAsync(update);
        Assert.Equal(original.Id, saved.Id);
        Assert.Equal(original.CreatedAt, saved.CreatedAt);
        Assert.Equal("Renamed", saved.GameName);
        Assert.Equal("NEW", saved.TagLine);
        Assert.Equal("other-region", saved.Region);
        Assert.Equal(later, saved.LastUpdatedAt);
        var loaded = await repository.FindByPuuidAsync(original.Puuid);
        Assert.Equal(saved.GameName, loaded!.GameName);
    }

    [Fact]
    public async Task New_player_roundtrips_all_fields_in_PostgreSQL()
    {
        await using var db = postgres.CreateContext();
        var repository = new PlayerRepository(db);
        var input = Create();
        var saved = await repository.SaveAsync(input);
        var loaded = await repository.FindByPuuidAsync(input.Puuid);
        Assert.NotNull(loaded);
        Assert.Equal(input.Id, saved.Id);
        Assert.Equal(input.Id, loaded.Id);
        Assert.Equal(input.Puuid, loaded.Puuid);
        Assert.Equal(input.GameName, loaded.GameName);
        Assert.Equal(input.TagLine, loaded.TagLine);
        Assert.Equal(input.Region, loaded.Region);
        Assert.Equal(input.CreatedAt, loaded.CreatedAt);
        Assert.Equal(input.LastUpdatedAt, loaded.LastUpdatedAt);
    }
}

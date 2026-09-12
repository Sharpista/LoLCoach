using LoLCoach.Api.Domain;
using LoLCoach.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LoLCoach.Tests;

public sealed class PlayerRepositoryTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static Player Create(string? puuid = null, string name = "Example", string tagLine = "TAG",
        string region = "opaque-region", DateTimeOffset? timestamp = null, DateTimeOffset? lastUpdatedAt = null) => new(
        Guid.NewGuid(), puuid ?? Guid.NewGuid().ToString("N"), name, tagLine, region,
        timestamp ?? DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        lastUpdatedAt ?? timestamp ?? DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

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
    public async Task Concurrent_upserts_return_the_values_from_their_own_operations()
    {
        var puuid = Guid.NewGuid().ToString("N");
        var createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var inputs = Enumerable.Range(0, 12)
            .Select(index => Create(puuid, $"Name{index}", $"Tag{index}", $"region-{index}", createdAt,
                DateTimeOffset.Parse($"2026-02-{index + 1:00}T00:00:00Z")))
            .ToArray();

        var results = await Task.WhenAll(inputs.Select(async input =>
        {
            await using var db = postgres.CreateContext();
            return await new PlayerRepository(db).SaveAsync(input);
        }));

        var persistedId = Assert.Single(results.Select(player => player.Id).Distinct());
        Assert.All(results.Zip(inputs), pair =>
        {
            var (result, input) = pair;
            Assert.Equal(persistedId, result.Id);
            Assert.Equal(input.Puuid, result.Puuid);
            Assert.Equal(input.GameName, result.GameName);
            Assert.Equal(input.TagLine, result.TagLine);
            Assert.Equal(input.Region, result.Region);
            Assert.Equal(input.CreatedAt, result.CreatedAt);
            Assert.Equal(input.LastUpdatedAt, result.LastUpdatedAt);
        });

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

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;
using LoLCoach.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LoLCoach.Tests;

public sealed class MatchSyncEndpointTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private sealed class FakeRiotMatchClient : IRiotMatchClient
    {
        public IReadOnlyList<string> MatchIds { get; set; } = [];
        public Dictionary<string, RiotMatchDetails> Matches { get; } = [];

        public Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, string platform, int count,
            CancellationToken cancellationToken = default)
            => Task.FromResult(MatchIds);

        public Task<RiotMatchDetails> GetMatchAsync(string matchId, string platform, CancellationToken cancellationToken = default)
            => Task.FromResult(Matches[matchId]);
    }

    private WebApplicationFactory<Program> CreateFactory(FakeRiotMatchClient matchClient)
    {
        var connectionString = postgres.ConnectionString;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:LoLCoach"] = connectionString }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRiotMatchClient>();
                services.AddSingleton<IRiotMatchClient>(matchClient);
            });
        });
    }

    [Fact]
    public async Task Sync_valid_player_returns_import_counters_and_persists_match()
    {
        var player = await SavePlayerAsync("endpoint-puuid");
        var riot = new FakeRiotMatchClient { MatchIds = ["BR1_endpoint"] };
        riot.Matches["BR1_endpoint"] = CreateDetails("BR1_endpoint", player.Puuid);
        await using var factory = CreateFactory(riot);
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/players/{player.Id}/matches/sync", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, json.RootElement.GetProperty("imported").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("skipped").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("failed").GetInt32());

        await using var db = postgres.CreateContext();
        Assert.True(await db.Matches.AnyAsync(match => match.RiotMatchId == "BR1_endpoint"));
        Assert.Equal(1, await db.PlayerMatches.CountAsync(match => match.PlayerId == player.Id));
    }

    [Fact]
    public async Task Sync_unknown_player_returns_404_problem_details()
    {
        await using var factory = CreateFactory(new FakeRiotMatchClient());
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/players/{Guid.NewGuid()}/matches/sync", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Player not found", json.RootElement.GetProperty("title").GetString());
    }

    private async Task<Player> SavePlayerAsync(string puuid)
    {
        await using var db = postgres.CreateContext();
        var player = new Player(Guid.NewGuid(), puuid, "Example", "TAG", "br1",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        return await new PlayerRepository(db).SaveAsync(player);
    }

    private static RiotMatchDetails CreateDetails(string matchId, string puuid) => new(
        new RiotMatchMetadata(matchId, [puuid]),
        new RiotMatchInfo(1779999999000, 1780000000000, 1800, 420, "CLASSIC",
        [
            new RiotMatchParticipant(puuid, 64, "LeeSin", "JUNGLE", true, 11, 2, 13, 20, 140, 12000, 33000, 21000, 29, 12, 4),
        ]));
}

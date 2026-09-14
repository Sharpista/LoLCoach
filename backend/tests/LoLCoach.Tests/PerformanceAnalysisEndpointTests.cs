using System.Net;
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

public sealed class PerformanceAnalysisEndpointTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private sealed class FakeRiotMatchClient : IRiotMatchClient
    {
        public Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, string platform, int count,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<RiotMatchDetails> GetMatchAsync(string matchId, string platform,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var connectionString = postgres.ConnectionString;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:LoLCoach"] = connectionString }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRiotMatchClient>();
                services.AddSingleton<IRiotMatchClient, FakeRiotMatchClient>();
            });
        });
    }

    [Fact]
    public async Task Analysis_returns_summary_top_insights_champions_and_recent_matches()
    {
        var player = await SavePlayerWithMatchesAsync();
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/players/{player.Id}/analysis");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(player.Id, root.GetProperty("player").GetProperty("id").GetGuid());
        Assert.Equal(3, root.GetProperty("summary").GetProperty("matchesAnalysed").GetInt32());
        Assert.Equal(33.33m, root.GetProperty("summary").GetProperty("winrate").GetDecimal());
        var insights = root.GetProperty("insights").EnumerateArray().ToList();
        Assert.Equal(3, insights.Count);
        Assert.Contains(insights, insight =>
            insight.GetProperty("type").GetString() == "HIGH_DEATHS" &&
            insight.GetProperty("metric").GetString() == "averageDeaths");
        Assert.Equal(3, root.GetProperty("champions").GetArrayLength());
        Assert.Equal(3, root.GetProperty("recentMatches").GetArrayLength());
        Assert.Equal("Lux", root.GetProperty("recentMatches")[0].GetProperty("champion").GetString());
    }

    [Fact]
    public async Task Analysis_for_player_without_matches_returns_empty_collections()
    {
        var player = await SavePlayerAsync("empty-analysis-puuid");
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/players/{player.Id}/analysis");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, json.RootElement.GetProperty("summary").GetProperty("matchesAnalysed").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("insights").EnumerateArray());
        Assert.Empty(json.RootElement.GetProperty("champions").EnumerateArray());
        Assert.Empty(json.RootElement.GetProperty("recentMatches").EnumerateArray());
    }

    [Fact]
    public async Task Analysis_unknown_player_returns_404_problem_details()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/players/{Guid.NewGuid()}/analysis");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Player not found", json.RootElement.GetProperty("title").GetString());
    }

    private async Task<Player> SavePlayerWithMatchesAsync()
    {
        var player = await SavePlayerAsync("analysis-puuid");
        await using var db = postgres.CreateContext();
        db.Matches.Add(CreateMatch("BR1_analysis_1", player.Id, "Ahri", false, 1, 8, 3, 110, 9_000, 6,
            1800, DateTimeOffset.Parse("2026-09-01T00:00:00Z")));
        db.Matches.Add(CreateMatch("BR1_analysis_2", player.Id, "Jinx", true, 3, 7, 4, 125, 10_500, 7,
            1800, DateTimeOffset.Parse("2026-09-02T00:00:00Z")));
        db.Matches.Add(CreateMatch("BR1_analysis_3", player.Id, "Lux", false, 2, 9, 5, 100, 8_000, 5,
            1800, DateTimeOffset.Parse("2026-09-03T00:00:00Z")));
        await db.SaveChangesAsync();
        return player;
    }

    private async Task<Player> SavePlayerAsync(string puuid)
    {
        await using var db = postgres.CreateContext();
        var player = new Player(Guid.NewGuid(), puuid, "Example", "TAG", "br1",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"), DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        return await new PlayerRepository(db).SaveAsync(player);
    }

    private static Match CreateMatch(string riotMatchId, Guid playerId, string champion, bool win, int kills, int deaths,
        int assists, int cs, int damage, int vision, int durationSeconds, DateTimeOffset playedAt)
    {
        var match = new Match(Guid.NewGuid(), riotMatchId, playedAt, durationSeconds, 420, "CLASSIC");
        match.AddPlayerMatch(new PlayerMatch(Guid.NewGuid(), playerId, 1, champion, "MID", win, kills, deaths,
            assists, cs, 10_000, damage, 15_000, vision, 10, 3));
        return match;
    }
}

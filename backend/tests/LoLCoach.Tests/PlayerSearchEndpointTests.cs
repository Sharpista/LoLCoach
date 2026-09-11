using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LoLCoach.Api.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LoLCoach.Tests;

public sealed class PlayerSearchEndpointTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private sealed class FakeRiotAccountClient : IRiotAccountClient
    {
        public Func<string, string, string, CancellationToken, Task<RiotAccount>> Handler { get; set; } = null!;

        public Task<RiotAccount> GetAccountAsync(string gameName, string tagLine, string platform,
            CancellationToken cancellationToken = default)
            => Handler(gameName, tagLine, platform, cancellationToken);
    }

    private WebApplicationFactory<Program> CreateFactory(
        Func<string, string, string, CancellationToken, Task<RiotAccount>> riotHandler)
    {
        var connectionString = postgres.ConnectionString;
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:LoLCoach"] = connectionString }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRiotAccountClient>();
                services.AddSingleton<IRiotAccountClient>(new FakeRiotAccountClient { Handler = riotHandler });
            });
        });
    }

    [Fact]
    public async Task Search_valid_returns_200_with_camelCase_player_contract()
    {
        await using var factory = CreateFactory((_, _, _, _) =>
            Task.FromResult(new RiotAccount("puuid-happy", "Example", "TAG")));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/players/search",
            new { gameName = "Example", tagLine = "TAG", region = "br1" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(new[] { "createdAt", "gameName", "id", "lastUpdatedAt", "puuid", "region", "tagLine" },
            root.EnumerateObject().Select(property => property.Name).Order().ToArray());
        Assert.Equal("puuid-happy", root.GetProperty("puuid").GetString());
        Assert.Equal("Example", root.GetProperty("gameName").GetString());
        Assert.Equal("TAG", root.GetProperty("tagLine").GetString());
        Assert.Equal("br1", root.GetProperty("region").GetString());
    }

    [Fact]
    public async Task Search_twice_same_account_returns_same_id()
    {
        await using var factory = CreateFactory((_, _, _, _) =>
            Task.FromResult(new RiotAccount("puuid-repeat", "Example", "TAG")));
        using var client = factory.CreateClient();
        var payload = new { gameName = "Example", tagLine = "TAG", region = "br1" };

        var first = await client.PostAsJsonAsync("/api/players/search", payload);
        var second = await client.PostAsJsonAsync("/api/players/search", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var firstId = JsonDocument.Parse(await first.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString();
        var secondId = JsonDocument.Parse(await second.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetString();
        Assert.Equal(firstId, secondId);
    }

    [Fact]
    public async Task Search_invalid_returns_400_with_camelCase_errors()
    {
        await using var factory = CreateFactory((_, _, _, _) =>
            Task.FromResult(new RiotAccount("never-called", "x", "y")));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/players/search",
            new { gameName = "ab", tagLine = "!", region = "xx1" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("gameName", out _));
        Assert.True(errors.TryGetProperty("tagLine", out _));
        Assert.True(errors.TryGetProperty("region", out _));
    }

    [Fact]
    public async Task Search_riot_404_returns_404_problem_details()
    {
        await using var factory = CreateFactory((_, _, _, _) => throw new RiotAccountNotFoundException());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/players/search",
            new { gameName = "Example", tagLine = "TAG", region = "br1" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("Player not found", root.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Search_riot_429_returns_429_with_retry_after()
    {
        await using var factory = CreateFactory((_, _, _, _) => throw new RiotRateLimitedException(30));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/players/search",
            new { gameName = "Example", tagLine = "TAG", region = "br1" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.NotNull(response.Headers.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(30), response.Headers.RetryAfter!.Delta);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(429, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Search_riot_5xx_returns_503_problem_details()
    {
        await using var factory = CreateFactory((_, _, _, _) => throw new RiotServiceUnavailableException());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/players/search",
            new { gameName = "Example", tagLine = "TAG", region = "br1" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(503, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Riot service unavailable", json.RootElement.GetProperty("title").GetString());
    }
}

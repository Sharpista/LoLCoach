using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace LoLCoach.Tests;

public sealed class SearchPlayerHandlerTests
{
    private sealed class FakeRiotAccountClient : IRiotAccountClient
    {
        public Func<string, string, string, CancellationToken, Task<RiotAccount>> Handler { get; set; } = null!;

        public Task<RiotAccount> GetAccountAsync(string gameName, string tagLine, string platform,
            CancellationToken cancellationToken = default)
            => Handler(gameName, tagLine, platform, cancellationToken);
    }

    private sealed class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly Dictionary<string, Player> _players = [];

        public int Count => _players.Count;

        public Task<Player?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_players.Values.SingleOrDefault(player => player.Id == id));

        public Task<Player?> FindByPuuidAsync(string puuid, CancellationToken cancellationToken = default)
            => Task.FromResult(_players.TryGetValue(puuid, out var player) ? player : null);

        public Task<Player> SaveAsync(Player player, CancellationToken cancellationToken = default)
        {
            if (_players.TryGetValue(player.Puuid, out var existing))
            {
                var updated = new Player(existing.Id, player.Puuid, player.GameName, player.TagLine,
                    player.Region, existing.CreatedAt, player.LastUpdatedAt);
                _players[player.Puuid] = updated;
                return Task.FromResult(updated);
            }

            _players[player.Puuid] = player;
            return Task.FromResult(player);
        }
    }

    private static readonly DateTimeOffset FixedNow = DateTimeOffset.Parse("2026-03-01T12:00:00Z");

    private static SearchPlayerHandler CreateHandler(FakeRiotAccountClient client, InMemoryPlayerRepository repository)
        => new(new SearchPlayerValidator(), client, repository, new FakeTimeProvider(FixedNow),
            NullLogger<SearchPlayerHandler>.Instance);

    private static SearchPlayerCommand ValidCommand() => new()
    {
        GameName = "Example",
        TagLine = "TAG",
        Region = "br1",
    };

    [Fact]
    public async Task New_player_is_created_and_returns_puuid()
    {
        var client = new FakeRiotAccountClient
        {
            Handler = (_, _, _, _) => Task.FromResult(new RiotAccount("puuid-1", "Example", "TAG")),
        };
        var repository = new InMemoryPlayerRepository();
        var handler = CreateHandler(client, repository);

        var dto = await handler.SearchAsync(ValidCommand());

        Assert.Equal("puuid-1", dto.Puuid);
        Assert.Equal("Example", dto.GameName);
        Assert.Equal("TAG", dto.TagLine);
        Assert.Equal("br1", dto.Region);
        Assert.Equal(FixedNow, dto.CreatedAt);
        Assert.Equal(FixedNow, dto.LastUpdatedAt);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public async Task Existing_player_is_updated_preserving_id_and_createdAt()
    {
        var client = new FakeRiotAccountClient
        {
            Handler = (_, _, _, _) => Task.FromResult(new RiotAccount("puuid-1", "Renamed", "NEW")),
        };
        var repository = new InMemoryPlayerRepository();
        var originalCreated = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var original = new Player(Guid.NewGuid(), "puuid-1", "Example", "TAG", "br1", originalCreated, originalCreated);
        await repository.SaveAsync(original);
        var handler = CreateHandler(client, repository);

        var dto = await handler.SearchAsync(ValidCommand());

        Assert.Equal(original.Id, dto.Id);
        Assert.Equal(originalCreated, dto.CreatedAt);
        Assert.Equal("Renamed", dto.GameName);
        Assert.Equal("NEW", dto.TagLine);
        Assert.Equal(FixedNow, dto.LastUpdatedAt);
        Assert.Equal(1, repository.Count);
    }

    [Fact]
    public async Task Handler_normalizes_input_before_calling_client()
    {
        string? receivedGameName = null;
        string? receivedTagLine = null;
        string? receivedPlatform = null;
        var client = new FakeRiotAccountClient
        {
            Handler = (gameName, tagLine, platform, _) =>
            {
                receivedGameName = gameName;
                receivedTagLine = tagLine;
                receivedPlatform = platform;
                return Task.FromResult(new RiotAccount("puuid-1", gameName, tagLine));
            },
        };
        var repository = new InMemoryPlayerRepository();
        var handler = CreateHandler(client, repository);

        await handler.SearchAsync(new SearchPlayerCommand
        {
            GameName = " Example ",
            TagLine = " TAG ",
            Region = " BR1 ",
        });

        Assert.Equal("Example", receivedGameName);
        Assert.Equal("TAG", receivedTagLine);
        Assert.Equal("br1", receivedPlatform);
    }

    [Fact]
    public async Task Invalid_command_throws_validation_exception_with_camelCase_keys()
    {
        var client = new FakeRiotAccountClient();
        var repository = new InMemoryPlayerRepository();
        var handler = CreateHandler(client, repository);

        var exception = await Assert.ThrowsAsync<SearchPlayerValidationException>(() =>
            handler.SearchAsync(new SearchPlayerCommand { GameName = "ab", TagLine = "!", Region = "xx1" }));

        Assert.Contains("gameName", exception.Errors.Keys);
        Assert.Contains("tagLine", exception.Errors.Keys);
        Assert.Contains("region", exception.Errors.Keys);
    }

    [Fact]
    public async Task Riot_not_found_is_propagated()
    {
        var client = new FakeRiotAccountClient
        {
            Handler = (_, _, _, _) => throw new RiotAccountNotFoundException(),
        };
        var handler = CreateHandler(client, new InMemoryPlayerRepository());

        await Assert.ThrowsAsync<RiotAccountNotFoundException>(() => handler.SearchAsync(ValidCommand()));
    }

    [Fact]
    public async Task Riot_rate_limit_is_propagated_with_retry_after()
    {
        var client = new FakeRiotAccountClient
        {
            Handler = (_, _, _, _) => throw new RiotRateLimitedException(30),
        };
        var handler = CreateHandler(client, new InMemoryPlayerRepository());

        var exception = await Assert.ThrowsAsync<RiotRateLimitedException>(() => handler.SearchAsync(ValidCommand()));
        Assert.Equal(30, exception.RetryAfterSeconds);
    }

    [Fact]
    public async Task Riot_unavailable_is_propagated()
    {
        var client = new FakeRiotAccountClient
        {
            Handler = (_, _, _, _) => throw new RiotServiceUnavailableException(),
        };
        var handler = CreateHandler(client, new InMemoryPlayerRepository());

        await Assert.ThrowsAsync<RiotServiceUnavailableException>(() => handler.SearchAsync(ValidCommand()));
    }
}

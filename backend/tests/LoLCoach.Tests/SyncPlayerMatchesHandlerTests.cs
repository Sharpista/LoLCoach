using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace LoLCoach.Tests;

public sealed class SyncPlayerMatchesHandlerTests
{
    private sealed class InMemoryPlayerRepository : IPlayerRepository
    {
        private readonly Dictionary<Guid, Player> _players = [];

        public void Add(Player player) => _players[player.Id] = player;

        public Task<Player?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_players.GetValueOrDefault(id));

        public Task<Player?> FindByPuuidAsync(string puuid, CancellationToken cancellationToken = default)
            => Task.FromResult(_players.Values.SingleOrDefault(player => player.Puuid == puuid));

        public Task<Player> SaveAsync(Player player, CancellationToken cancellationToken = default)
        {
            _players[player.Id] = player;
            return Task.FromResult(player);
        }
    }

    private sealed class InMemoryMatchRepository : IMatchRepository
    {
        private readonly HashSet<string> _existing = [];

        public int AddedCount { get; private set; }
        public IReadOnlyCollection<Match> Added { get; private set; } = [];

        public void SeedExisting(string matchId) => _existing.Add(matchId);

        public Task<bool> ExistsByRiotMatchIdAsync(string riotMatchId, CancellationToken cancellationToken = default)
            => Task.FromResult(_existing.Contains(riotMatchId));

        public Task AddAsync(Match match, CancellationToken cancellationToken = default)
        {
            _existing.Add(match.RiotMatchId);
            AddedCount++;
            Added = Added.Append(match).ToArray();
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeRiotMatchClient : IRiotMatchClient
    {
        public IReadOnlyList<string> MatchIds { get; set; } = [];
        public Dictionary<string, RiotMatchDetails> Matches { get; } = [];
        public HashSet<string> FailingDetails { get; } = [];
        public List<string> RequestedDetails { get; } = [];
        public int RequestedCount { get; private set; }

        public Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, string platform, int count,
            CancellationToken cancellationToken = default)
        {
            RequestedCount = count;
            return Task.FromResult(MatchIds);
        }

        public Task<RiotMatchDetails> GetMatchAsync(string matchId, string platform, CancellationToken cancellationToken = default)
        {
            RequestedDetails.Add(matchId);
            if (FailingDetails.Contains(matchId))
            {
                throw new RiotServiceUnavailableException();
            }

            return Task.FromResult(Matches[matchId]);
        }
    }

    private static readonly Player Player = new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "target-puuid",
        "Example",
        "TAG",
        "br1",
        DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
        DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

    [Fact]
    public async Task Sync_imports_new_match_and_skips_existing_without_fetching_details()
    {
        var players = new InMemoryPlayerRepository();
        players.Add(Player);
        var matches = new InMemoryMatchRepository();
        matches.SeedExisting("BR1_existing");
        var client = new FakeRiotMatchClient { MatchIds = ["BR1_existing", "BR1_new"] };
        client.Matches["BR1_new"] = CreateDetails("BR1_new", "target-puuid");
        var handler = CreateHandler(players, matches, client);

        var result = await handler.SyncAsync(new SyncPlayerMatchesCommand(Player.Id));

        Assert.Equal(new SyncPlayerMatchesResult(1, 1, 0), result);
        Assert.Equal(20, client.RequestedCount);
        Assert.Equal(["BR1_new"], client.RequestedDetails);
        Assert.Equal(1, matches.AddedCount);
        Assert.Equal("BR1_new", Assert.Single(matches.Added).RiotMatchId);
    }

    [Fact]
    public async Task Sync_continues_after_individual_match_failure()
    {
        var players = new InMemoryPlayerRepository();
        players.Add(Player);
        var matches = new InMemoryMatchRepository();
        var client = new FakeRiotMatchClient { MatchIds = ["BR1_fail", "BR1_ok"] };
        client.FailingDetails.Add("BR1_fail");
        client.Matches["BR1_ok"] = CreateDetails("BR1_ok", "target-puuid");
        var handler = CreateHandler(players, matches, client);

        var result = await handler.SyncAsync(new SyncPlayerMatchesCommand(Player.Id));

        Assert.Equal(new SyncPlayerMatchesResult(1, 0, 1), result);
        Assert.Equal(["BR1_fail", "BR1_ok"], client.RequestedDetails);
    }

    [Fact]
    public async Task Sync_counts_failure_when_player_participant_is_absent()
    {
        var players = new InMemoryPlayerRepository();
        players.Add(Player);
        var matches = new InMemoryMatchRepository();
        var client = new FakeRiotMatchClient { MatchIds = ["BR1_absent"] };
        client.Matches["BR1_absent"] = CreateDetails("BR1_absent", "other-puuid");
        var handler = CreateHandler(players, matches, client);

        var result = await handler.SyncAsync(new SyncPlayerMatchesCommand(Player.Id));

        Assert.Equal(new SyncPlayerMatchesResult(0, 0, 1), result);
        Assert.Equal(0, matches.AddedCount);
    }

    [Fact]
    public async Task Sync_unknown_player_throws_not_found()
    {
        var handler = CreateHandler(new InMemoryPlayerRepository(), new InMemoryMatchRepository(), new FakeRiotMatchClient());

        await Assert.ThrowsAsync<PlayerNotFoundException>(() => handler.SyncAsync(new SyncPlayerMatchesCommand(Guid.NewGuid())));
    }

    private static SyncPlayerMatchesHandler CreateHandler(InMemoryPlayerRepository players, InMemoryMatchRepository matches,
        FakeRiotMatchClient client) => new(players, matches, client, new MatchNormalizer(),
        NullLogger<SyncPlayerMatchesHandler>.Instance);

    private static RiotMatchDetails CreateDetails(string matchId, string puuid) => new(
        new RiotMatchMetadata(matchId, [puuid]),
        new RiotMatchInfo(1779999999000, 1780000000000, 1800, 420, "CLASSIC",
        [
            new RiotMatchParticipant(puuid, 64, "LeeSin", "JUNGLE", true, 11, 2, 13, 20, 140, 12000, 33000, 21000, 29, 12, 4),
        ]));
}

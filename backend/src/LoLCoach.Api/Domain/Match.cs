namespace LoLCoach.Api.Domain;

public sealed class Match
{
    public Guid Id { get; private set; }
    public string RiotMatchId { get; private set; } = null!;
    public DateTimeOffset GameStart { get; private set; }
    public int GameDuration { get; private set; }
    public int QueueId { get; private set; }
    public string GameMode { get; private set; } = null!;

    private readonly List<PlayerMatch> _playerMatches = [];
    public IReadOnlyCollection<PlayerMatch> PlayerMatches => _playerMatches;

    private Match() { }

    public Match(Guid id, string riotMatchId, DateTimeOffset gameStart, int gameDuration, int queueId, string gameMode)
    {
        Id = id;
        RiotMatchId = riotMatchId;
        GameStart = gameStart;
        GameDuration = gameDuration;
        QueueId = queueId;
        GameMode = gameMode;
    }

    public void AddPlayerMatch(PlayerMatch playerMatch)
    {
        playerMatch.AttachToMatch(this);
        _playerMatches.Add(playerMatch);
    }
}

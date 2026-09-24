namespace LoLCoach.Api.Domain;

public sealed class PlayerMatch
{
    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public Match Match { get; private set; } = null!;
    public Guid PlayerId { get; private set; }
    public Player Player { get; private set; } = null!;
    public int ChampionId { get; private set; }
    public string ChampionName { get; private set; } = null!;
    public string TeamPosition { get; private set; } = null!;
    public bool Win { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public int Assists { get; private set; }
    public int TotalCs { get; private set; }
    public int GoldEarned { get; private set; }
    public int DamageToChampions { get; private set; }
    public int DamageTaken { get; private set; }
    public int VisionScore { get; private set; }
    public int WardsPlaced { get; private set; }
    public int WardsKilled { get; private set; }

    private PlayerMatch() { }

    public PlayerMatch(Guid id, Guid playerId, int championId, string championName, string teamPosition, bool win,
        int kills, int deaths, int assists, int totalCs, int goldEarned, int damageToChampions, int damageTaken,
        int visionScore, int wardsPlaced, int wardsKilled)
    {
        Id = id;
        PlayerId = playerId;
        ChampionId = championId;
        ChampionName = championName;
        TeamPosition = teamPosition;
        Win = win;
        Kills = kills;
        Deaths = deaths;
        Assists = assists;
        TotalCs = totalCs;
        GoldEarned = goldEarned;
        DamageToChampions = damageToChampions;
        DamageTaken = damageTaken;
        VisionScore = visionScore;
        WardsPlaced = wardsPlaced;
        WardsKilled = wardsKilled;
    }

    internal void AttachToMatch(Match match)
    {
        Match = match;
        MatchId = match.Id;
    }
}

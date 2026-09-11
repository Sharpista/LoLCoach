namespace LoLCoach.Api.Domain;

public sealed class Player
{
    public Guid Id { get; private set; }
    public string Puuid { get; private set; } = null!;
    public string GameName { get; private set; } = null!;
    public string TagLine { get; private set; } = null!;
    public string Region { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }

    private Player() { }

    public Player(Guid id, string puuid, string gameName, string tagLine, string region,
        DateTimeOffset createdAt, DateTimeOffset lastUpdatedAt)
    {
        Id = id;
        Puuid = puuid;
        GameName = gameName;
        TagLine = tagLine;
        Region = region;
        CreatedAt = createdAt;
        LastUpdatedAt = lastUpdatedAt;
    }
}

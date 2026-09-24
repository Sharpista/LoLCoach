namespace LoLCoach.Api.Application;

public interface IRiotMatchClient
{
    Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, string platform, int count,
        CancellationToken cancellationToken = default);

    Task<RiotMatchDetails> GetMatchAsync(string matchId, string platform, CancellationToken cancellationToken = default);
}

public sealed record RiotMatchDetails(RiotMatchMetadata Metadata, RiotMatchInfo Info);

public sealed record RiotMatchMetadata(string MatchId, IReadOnlyList<string> Participants);

public sealed record RiotMatchInfo(
    long GameCreation,
    long GameStartTimestamp,
    long GameDuration,
    int QueueId,
    string GameMode,
    IReadOnlyList<RiotMatchParticipant> Participants);

public sealed record RiotMatchParticipant(
    string Puuid,
    int ChampionId,
    string ChampionName,
    string TeamPosition,
    bool Win,
    int Kills,
    int Deaths,
    int Assists,
    int TotalMinionsKilled,
    int NeutralMinionsKilled,
    int GoldEarned,
    int TotalDamageDealtToChampions,
    int TotalDamageTaken,
    int VisionScore,
    int WardsPlaced,
    int WardsKilled);

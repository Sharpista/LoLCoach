using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Application;

public sealed class MatchNormalizer : IMatchNormalizer
{
    public MatchNormalizationResult? Normalize(RiotMatchDetails matchDetails, Player player)
    {
        var participant = matchDetails.Info.Participants.SingleOrDefault(value => value.Puuid == player.Puuid);
        if (participant is null)
        {
            return null;
        }

        var startMilliseconds = matchDetails.Info.GameStartTimestamp > 0
            ? matchDetails.Info.GameStartTimestamp
            : matchDetails.Info.GameCreation;
        var match = new Match(
            Guid.NewGuid(),
            matchDetails.Metadata.MatchId,
            DateTimeOffset.FromUnixTimeMilliseconds(startMilliseconds),
            checked((int)matchDetails.Info.GameDuration),
            matchDetails.Info.QueueId,
            matchDetails.Info.GameMode);

        var playerMatch = new PlayerMatch(
            Guid.NewGuid(),
            player.Id,
            participant.ChampionId,
            participant.ChampionName,
            participant.TeamPosition,
            participant.Win,
            participant.Kills,
            participant.Deaths,
            participant.Assists,
            participant.TotalMinionsKilled + participant.NeutralMinionsKilled,
            participant.GoldEarned,
            participant.TotalDamageDealtToChampions,
            participant.TotalDamageTaken,
            participant.VisionScore,
            participant.WardsPlaced,
            participant.WardsKilled);
        match.AddPlayerMatch(playerMatch);
        return new MatchNormalizationResult(match, playerMatch);
    }
}

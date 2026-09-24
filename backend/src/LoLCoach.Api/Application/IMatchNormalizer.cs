using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Application;

public interface IMatchNormalizer
{
    MatchNormalizationResult? Normalize(RiotMatchDetails matchDetails, Player player);
}

public sealed record MatchNormalizationResult(Match Match, PlayerMatch PlayerMatch);

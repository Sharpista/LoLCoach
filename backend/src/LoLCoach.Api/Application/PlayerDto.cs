using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Application;

public sealed record PlayerDto(Guid Id, string Puuid, string GameName, string TagLine, string Region,
    DateTimeOffset CreatedAt, DateTimeOffset LastUpdatedAt)
{
    public static PlayerDto FromPlayer(Player player) => new(player.Id, player.Puuid, player.GameName,
        player.TagLine, player.Region, player.CreatedAt, player.LastUpdatedAt);
}

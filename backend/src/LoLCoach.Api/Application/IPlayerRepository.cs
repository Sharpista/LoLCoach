using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Application;

public interface IPlayerRepository
{
    Task<Player?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Player?> FindByPuuidAsync(string puuid, CancellationToken cancellationToken = default);
    Task<Player> SaveAsync(Player player, CancellationToken cancellationToken = default);
}

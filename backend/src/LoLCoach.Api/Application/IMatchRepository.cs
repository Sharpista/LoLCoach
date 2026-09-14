using LoLCoach.Api.Domain;

namespace LoLCoach.Api.Application;

public interface IMatchRepository
{
    Task<bool> ExistsByRiotMatchIdAsync(string riotMatchId, CancellationToken cancellationToken = default);
    Task AddAsync(Match match, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

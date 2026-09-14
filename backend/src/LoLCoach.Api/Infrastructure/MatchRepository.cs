using LoLCoach.Api.Domain;
using LoLCoach.Api.Application;
using Microsoft.EntityFrameworkCore;

namespace LoLCoach.Api.Infrastructure;

public sealed class MatchRepository(PlayerDbContext db) : IMatchRepository
{
    public Task<bool> ExistsByRiotMatchIdAsync(string riotMatchId, CancellationToken cancellationToken = default)
        => db.Matches.AsNoTracking().AnyAsync(match => match.RiotMatchId == riotMatchId, cancellationToken);

    public async Task AddAsync(Match match, CancellationToken cancellationToken = default)
        => await db.Matches.AddAsync(match, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }
}

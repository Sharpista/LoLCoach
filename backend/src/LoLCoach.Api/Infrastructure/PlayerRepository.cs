using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoLCoach.Api.Infrastructure;

public sealed class PlayerRepository(PlayerDbContext db) : IPlayerRepository
{
    public Task<Player?> FindByPuuidAsync(string puuid, CancellationToken cancellationToken = default)
        => db.Players.AsNoTracking().SingleOrDefaultAsync(player => player.Puuid == puuid, cancellationToken);

    public async Task<Player> SaveAsync(Player player, CancellationToken cancellationToken = default)
    {
        // FromSql keeps every interpolated value in a database parameter.
        // Materialize before Single: INSERT ... RETURNING cannot be composed as a subquery.
        var saved = await db.Players.FromSql($"""
            INSERT INTO players (id, puuid, game_name, tag_line, region, created_at, last_updated_at)
            VALUES ({player.Id}, {player.Puuid}, {player.GameName}, {player.TagLine},
                    {player.Region}, {player.CreatedAt}, {player.LastUpdatedAt})
            ON CONFLICT (puuid) DO UPDATE SET
                game_name = EXCLUDED.game_name,
                tag_line = EXCLUDED.tag_line,
                region = EXCLUDED.region,
                last_updated_at = EXCLUDED.last_updated_at
            RETURNING id, puuid, game_name, tag_line, region, created_at, last_updated_at
            """).AsNoTracking().ToListAsync(cancellationToken);
        return saved.Single();
    }
}

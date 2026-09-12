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
        // Keep the write and read as separate commands. Supabase's pooler can apply an
        // INSERT ... RETURNING and then time out while forwarding the result set, which
        // makes the caller observe a failure even though the upsert was committed.
        // ExecuteNonQuery avoids that result-set path; the subsequent SELECT reads the
        // database as the source of truth. The atomic ON CONFLICT upsert and unique index
        // still prevent duplicate PUUIDs under concurrent requests.
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO players (id, puuid, game_name, tag_line, region, created_at, last_updated_at)
            VALUES ({player.Id}, {player.Puuid}, {player.GameName}, {player.TagLine},
                    {player.Region}, {player.CreatedAt}, {player.LastUpdatedAt})
            ON CONFLICT (puuid) DO UPDATE SET
                game_name = EXCLUDED.game_name,
                tag_line = EXCLUDED.tag_line,
                region = EXCLUDED.region,
                last_updated_at = EXCLUDED.last_updated_at
            """, cancellationToken);

        return await db.Players.AsNoTracking()
            .SingleAsync(saved => saved.Puuid == player.Puuid, cancellationToken);
    }
}

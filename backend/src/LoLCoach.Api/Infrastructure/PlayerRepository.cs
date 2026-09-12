using System.Data;
using LoLCoach.Api.Application;
using LoLCoach.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace LoLCoach.Api.Infrastructure;

public sealed class PlayerRepository(PlayerDbContext db) : IPlayerRepository
{
    public Task<Player?> FindByPuuidAsync(string puuid, CancellationToken cancellationToken = default)
        => db.Players.AsNoTracking().SingleOrDefaultAsync(player => player.Puuid == puuid, cancellationToken);

    public async Task<Player> SaveAsync(Player player, CancellationToken cancellationToken = default)
    {
        // Keep the upsert and its response in one PostgreSQL statement. RETURNING is
        // evaluated for this command before another upsert can change the row, so the
        // result cannot be crossed with a concurrent request's later SELECT.
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State == ConnectionState.Closed;
        if (openedHere)
            await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            if (db.Database.CurrentTransaction is not null)
                command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();

            command.CommandText = """
                INSERT INTO players (id, puuid, game_name, tag_line, region, created_at, last_updated_at)
                VALUES (@id, @puuid, @game_name, @tag_line, @region, @created_at, @last_updated_at)
                ON CONFLICT (puuid) DO UPDATE SET
                    game_name = EXCLUDED.game_name,
                    tag_line = EXCLUDED.tag_line,
                    region = EXCLUDED.region,
                    last_updated_at = EXCLUDED.last_updated_at
                RETURNING id, puuid, game_name, tag_line, region, created_at, last_updated_at;
                """;
            command.Parameters.Add(new NpgsqlParameter("id", player.Id));
            command.Parameters.Add(new NpgsqlParameter("puuid", player.Puuid));
            command.Parameters.Add(new NpgsqlParameter("game_name", player.GameName));
            command.Parameters.Add(new NpgsqlParameter("tag_line", player.TagLine));
            command.Parameters.Add(new NpgsqlParameter("region", player.Region));
            command.Parameters.Add(new NpgsqlParameter("created_at", player.CreatedAt));
            command.Parameters.Add(new NpgsqlParameter("last_updated_at", player.LastUpdatedAt));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException("The player upsert did not return a player.");

            return new Player(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5),
                reader.GetFieldValue<DateTimeOffset>(6));
        }
        finally
        {
            if (openedHere)
                await db.Database.CloseConnectionAsync();
        }
    }
}

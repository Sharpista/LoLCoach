using LoLCoach.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoLCoach.Api.Infrastructure;

public sealed class PlayerDbContext(DbContextOptions<PlayerDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var player = modelBuilder.Entity<Player>();
        player.ToTable("players");
        player.HasKey(value => value.Id).HasName("pk_players");
        player.Property(value => value.Id).HasColumnName("id").ValueGeneratedNever();
        player.Property(value => value.Puuid).HasColumnName("puuid").IsRequired();
        player.Property(value => value.GameName).HasColumnName("game_name").IsRequired();
        player.Property(value => value.TagLine).HasColumnName("tag_line").IsRequired();
        player.Property(value => value.Region).HasColumnName("region").IsRequired();
        player.Property(value => value.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        player.Property(value => value.LastUpdatedAt).HasColumnName("last_updated_at").HasColumnType("timestamp with time zone");
        player.HasIndex(value => value.Puuid).IsUnique().HasDatabaseName("ix_players_puuid");
    }
}

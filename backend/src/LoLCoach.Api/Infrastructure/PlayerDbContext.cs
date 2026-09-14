using LoLCoach.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoLCoach.Api.Infrastructure;

public sealed class PlayerDbContext(DbContextOptions<PlayerDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<PlayerMatch> PlayerMatches => Set<PlayerMatch>();

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

        var match = modelBuilder.Entity<Match>();
        match.ToTable("matches");
        match.HasKey(value => value.Id).HasName("pk_matches");
        match.Property(value => value.Id).HasColumnName("id").ValueGeneratedNever();
        match.Property(value => value.RiotMatchId).HasColumnName("riot_match_id").IsRequired();
        match.Property(value => value.GameStart).HasColumnName("game_start").HasColumnType("timestamp with time zone");
        match.Property(value => value.GameDuration).HasColumnName("game_duration");
        match.Property(value => value.QueueId).HasColumnName("queue_id");
        match.Property(value => value.GameMode).HasColumnName("game_mode").IsRequired();
        match.HasIndex(value => value.RiotMatchId).IsUnique().HasDatabaseName("ix_matches_riot_match_id");
        match.HasMany(value => value.PlayerMatches).WithOne(value => value.Match).HasForeignKey(value => value.MatchId)
            .HasConstraintName("fk_player_matches_matches_match_id").OnDelete(DeleteBehavior.Cascade);

        var playerMatch = modelBuilder.Entity<PlayerMatch>();
        playerMatch.ToTable("player_matches");
        playerMatch.HasKey(value => value.Id).HasName("pk_player_matches");
        playerMatch.Property(value => value.Id).HasColumnName("id").ValueGeneratedNever();
        playerMatch.Property(value => value.MatchId).HasColumnName("match_id");
        playerMatch.Property(value => value.PlayerId).HasColumnName("player_id");
        playerMatch.Property(value => value.ChampionId).HasColumnName("champion_id");
        playerMatch.Property(value => value.ChampionName).HasColumnName("champion_name").IsRequired();
        playerMatch.Property(value => value.TeamPosition).HasColumnName("team_position").IsRequired();
        playerMatch.Property(value => value.Win).HasColumnName("win");
        playerMatch.Property(value => value.Kills).HasColumnName("kills");
        playerMatch.Property(value => value.Deaths).HasColumnName("deaths");
        playerMatch.Property(value => value.Assists).HasColumnName("assists");
        playerMatch.Property(value => value.TotalCs).HasColumnName("total_cs");
        playerMatch.Property(value => value.GoldEarned).HasColumnName("gold_earned");
        playerMatch.Property(value => value.DamageToChampions).HasColumnName("damage_to_champions");
        playerMatch.Property(value => value.DamageTaken).HasColumnName("damage_taken");
        playerMatch.Property(value => value.VisionScore).HasColumnName("vision_score");
        playerMatch.Property(value => value.WardsPlaced).HasColumnName("wards_placed");
        playerMatch.Property(value => value.WardsKilled).HasColumnName("wards_killed");
        playerMatch.HasOne(value => value.Player).WithMany(value => value.PlayerMatches).HasForeignKey(value => value.PlayerId)
            .HasConstraintName("fk_player_matches_players_player_id").OnDelete(DeleteBehavior.Cascade);
        playerMatch.HasIndex(value => value.MatchId).HasDatabaseName("ix_player_matches_match_id");
        playerMatch.HasIndex(value => value.PlayerId).HasDatabaseName("ix_player_matches_player_id");
    }
}

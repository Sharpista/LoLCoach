using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoLCoach.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    riot_match_id = table.Column<string>(type: "text", nullable: false),
                    game_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    game_duration = table.Column<int>(type: "integer", nullable: false),
                    queue_id = table.Column<int>(type: "integer", nullable: false),
                    game_mode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    champion_id = table.Column<int>(type: "integer", nullable: false),
                    champion_name = table.Column<string>(type: "text", nullable: false),
                    team_position = table.Column<string>(type: "text", nullable: false),
                    win = table.Column<bool>(type: "boolean", nullable: false),
                    kills = table.Column<int>(type: "integer", nullable: false),
                    deaths = table.Column<int>(type: "integer", nullable: false),
                    assists = table.Column<int>(type: "integer", nullable: false),
                    total_cs = table.Column<int>(type: "integer", nullable: false),
                    gold_earned = table.Column<int>(type: "integer", nullable: false),
                    damage_to_champions = table.Column<int>(type: "integer", nullable: false),
                    damage_taken = table.Column<int>(type: "integer", nullable: false),
                    vision_score = table.Column<int>(type: "integer", nullable: false),
                    wards_placed = table.Column<int>(type: "integer", nullable: false),
                    wards_killed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_matches", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_matches_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_player_matches_players_player_id",
                        column: x => x.player_id,
                        principalTable: "players",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_matches_riot_match_id",
                table: "matches",
                column: "riot_match_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_matches_match_id",
                table: "player_matches",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_matches_player_id",
                table: "player_matches",
                column: "player_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_matches");

            migrationBuilder.DropTable(
                name: "matches");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoLCoach.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlayers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    puuid = table.Column<string>(type: "text", nullable: false),
                    game_name = table.Column<string>(type: "text", nullable: false),
                    tag_line = table.Column<string>(type: "text", nullable: false),
                    region = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_players_puuid",
                table: "players",
                column: "puuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "players");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayersAndPlayerFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_long_term_tips_target_shape",
                table: "long_term_tips");

            // Free-text top-scorer tips no longer map to anything (we switched to a
            // strict player FK). The tournament hasn't started yet, so this only
            // affects pre-launch / dev test rows. Tournament.top_scorer_name is
            // dropped right after so no production data is at risk either.
            migrationBuilder.Sql("DELETE FROM long_term_tips WHERE type = 'TopScorer';");

            migrationBuilder.DropColumn(
                name: "top_scorer_name",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "target_player_name",
                table: "long_term_tips");

            migrationBuilder.AddColumn<Guid>(
                name: "top_scorer_player_id",
                table: "tournaments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "target_player_id",
                table: "long_term_tips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    national_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.id);
                    table.ForeignKey(
                        name: "FK_players_teams_national_national_team_id",
                        column: x => x.national_team_id,
                        principalTable: "teams_national",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_top_scorer_player_id",
                table: "tournaments",
                column: "top_scorer_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_long_term_tips_target_player_id",
                table: "long_term_tips",
                column: "target_player_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_long_term_tips_target_shape",
                table: "long_term_tips",
                sql: "(type = 'Winner' AND target_team_id IS NOT NULL AND target_player_id IS NULL) OR (type = 'TopScorer' AND target_player_id IS NOT NULL AND target_team_id IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_players_national_team_id",
                table: "players",
                column: "national_team_id");

            migrationBuilder.CreateIndex(
                name: "ux_players_team_name",
                table: "players",
                columns: new[] { "national_team_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_long_term_tips_players_target_player_id",
                table: "long_term_tips",
                column: "target_player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tournaments_players_top_scorer_player_id",
                table: "tournaments",
                column: "top_scorer_player_id",
                principalTable: "players",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_long_term_tips_players_target_player_id",
                table: "long_term_tips");

            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_players_top_scorer_player_id",
                table: "tournaments");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropIndex(
                name: "IX_tournaments_top_scorer_player_id",
                table: "tournaments");

            migrationBuilder.DropIndex(
                name: "IX_long_term_tips_target_player_id",
                table: "long_term_tips");

            migrationBuilder.DropCheckConstraint(
                name: "ck_long_term_tips_target_shape",
                table: "long_term_tips");

            migrationBuilder.DropColumn(
                name: "top_scorer_player_id",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "target_player_id",
                table: "long_term_tips");

            migrationBuilder.AddColumn<string>(
                name: "top_scorer_name",
                table: "tournaments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "target_player_name",
                table: "long_term_tips",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_long_term_tips_target_shape",
                table: "long_term_tips",
                sql: "(type = 'Winner' AND target_team_id IS NOT NULL AND target_player_name IS NULL) OR (type = 'TopScorer' AND target_player_name IS NOT NULL AND target_team_id IS NULL)");
        }
    }
}

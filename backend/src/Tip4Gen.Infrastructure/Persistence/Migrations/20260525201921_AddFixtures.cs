using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixtures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams_national",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_national", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tournaments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    external_league_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    season = table.Column<int>(type: "integer", nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tournaments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tournament_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    stage = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    group_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    round_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    home_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    away_team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kickoff_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    home_goals = table.Column<int>(type: "integer", nullable: true),
                    away_goals = table.Column<int>(type: "integer", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.id);
                    table.CheckConstraint("ck_matches_score_nullability", "(home_goals IS NULL) = (away_goals IS NULL)");
                    table.CheckConstraint("ck_matches_stage", "stage IN ('Group','R32','R16','QF','SF','Bronze','Final')");
                    table.CheckConstraint("ck_matches_status", "status IN ('Scheduled','Live','Finished','Postponed','Cancelled','Abandoned','Awarded')");
                    table.CheckConstraint("ck_matches_teams_distinct", "home_team_id <> away_team_id");
                    table.ForeignKey(
                        name: "FK_matches_teams_national_away_team_id",
                        column: x => x.away_team_id,
                        principalTable: "teams_national",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matches_teams_national_home_team_id",
                        column: x => x.home_team_id,
                        principalTable: "teams_national",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matches_tournaments_tournament_id",
                        column: x => x.tournament_id,
                        principalTable: "tournaments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_matches_away_team_id",
                table: "matches",
                column: "away_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_matches_home_team_id",
                table: "matches",
                column: "home_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_matches_kickoff_utc",
                table: "matches",
                column: "kickoff_utc");

            migrationBuilder.CreateIndex(
                name: "IX_matches_status",
                table: "matches",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_matches_tournament_id_external_id",
                table: "matches",
                columns: new[] { "tournament_id", "external_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_teams_national_external_id",
                table: "teams_national",
                column: "external_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_external_league_id_season",
                table: "tournaments",
                columns: new[] { "external_league_id", "season" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "teams_national");

            migrationBuilder.DropTable(
                name: "tournaments");
        }
    }
}

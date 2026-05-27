using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scored_tips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tip_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    base_points = table.Column<int>(type: "integer", nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(3,1)", nullable: false),
                    joker_applied = table.Column<bool>(type: "boolean", nullable: false),
                    final_points = table.Column<int>(type: "integer", nullable: false),
                    scored_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scored_tips", x => x.id);
                    table.CheckConstraint("ck_scored_tips_category", "category IN ('Nothing','OneTeamGoals','Winner','WinnerAndGoalDiff','Exact')");
                    table.CheckConstraint("ck_scored_tips_multiplier_range", "multiplier >= 1 AND multiplier <= 3");
                    table.CheckConstraint("ck_scored_tips_points_non_negative", "base_points >= 0 AND final_points >= 0");
                    table.ForeignKey(
                        name: "FK_scored_tips_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scored_tips_tips_tip_id",
                        column: x => x.tip_id,
                        principalTable: "tips",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_scored_tips_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_scored_tips_match_id",
                table: "scored_tips",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "IX_scored_tips_tip_id",
                table: "scored_tips",
                column: "tip_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scored_tips_user_id",
                table: "scored_tips",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scored_tips");
        }
    }
}

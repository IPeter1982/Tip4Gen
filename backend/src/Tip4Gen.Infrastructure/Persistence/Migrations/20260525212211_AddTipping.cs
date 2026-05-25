using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    home_goals = table.Column<int>(type: "integer", nullable: false),
                    away_goals = table.Column<int>(type: "integer", nullable: false),
                    joker = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tips", x => x.id);
                    table.CheckConstraint("ck_tips_away_goals_range", "away_goals >= 0 AND away_goals <= 15");
                    table.CheckConstraint("ck_tips_home_goals_range", "home_goals >= 0 AND home_goals <= 15");
                    table.ForeignKey(
                        name: "FK_tips_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tips_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tips_match_id",
                table: "tips",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "IX_tips_user_id_joker",
                table: "tips",
                columns: new[] { "user_id", "joker" },
                filter: "joker = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_tips_user_id_match_id",
                table: "tips",
                columns: new[] { "user_id", "match_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tips");
        }
    }
}

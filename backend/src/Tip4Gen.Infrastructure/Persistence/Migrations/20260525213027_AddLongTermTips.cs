using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLongTermTips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "long_term_tips",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    target_team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_player_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_long_term_tips", x => x.id);
                    table.CheckConstraint("ck_long_term_tips_target_shape", "(type = 'Winner' AND target_team_id IS NOT NULL AND target_player_name IS NULL) OR (type = 'TopScorer' AND target_player_name IS NOT NULL AND target_team_id IS NULL)");
                    table.CheckConstraint("ck_long_term_tips_type", "type IN ('Winner','TopScorer')");
                    table.ForeignKey(
                        name: "FK_long_term_tips_teams_national_target_team_id",
                        column: x => x.target_team_id,
                        principalTable: "teams_national",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_long_term_tips_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_long_term_tips_target_team_id",
                table: "long_term_tips",
                column: "target_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_long_term_tips_user_id_type",
                table: "long_term_tips",
                columns: new[] { "user_id", "type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "long_term_tips");
        }
    }
}

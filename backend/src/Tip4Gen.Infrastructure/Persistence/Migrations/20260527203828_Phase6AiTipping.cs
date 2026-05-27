using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6AiTipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tips_user_id_match_id",
                table: "tips");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "tips",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "is_ai_fallback",
                table: "tips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "reasoning",
                table: "tips",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "team_member_id",
                table: "tips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "scored_tips",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "team_member_id",
                table: "scored_tips",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_tip_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_tip_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_ai_tip_attempts_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ai_tip_attempts_team_members_team_member_id",
                        column: x => x.team_member_id,
                        principalTable: "team_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tips_member_match",
                table: "tips",
                columns: new[] { "team_member_id", "match_id" },
                unique: true,
                filter: "team_member_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_tips_user_match",
                table: "tips",
                columns: new[] { "user_id", "match_id" },
                unique: true,
                filter: "user_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tips_ai_no_joker",
                table: "tips",
                sql: "team_member_id IS NULL OR joker = FALSE");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tips_owner_xor",
                table: "tips",
                sql: "(user_id IS NOT NULL AND team_member_id IS NULL) OR (user_id IS NULL AND team_member_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_scored_tips_team_member_id",
                table: "scored_tips",
                column: "team_member_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_scored_tips_owner_xor",
                table: "scored_tips",
                sql: "(user_id IS NOT NULL AND team_member_id IS NULL) OR (user_id IS NULL AND team_member_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ai_tip_attempts_match_id",
                table: "ai_tip_attempts",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "IX_ai_tip_attempts_team_member_id_match_id",
                table: "ai_tip_attempts",
                columns: new[] { "team_member_id", "match_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_scored_tips_team_members_team_member_id",
                table: "scored_tips",
                column: "team_member_id",
                principalTable: "team_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tips_team_members_team_member_id",
                table: "tips",
                column: "team_member_id",
                principalTable: "team_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_scored_tips_team_members_team_member_id",
                table: "scored_tips");

            migrationBuilder.DropForeignKey(
                name: "FK_tips_team_members_team_member_id",
                table: "tips");

            migrationBuilder.DropTable(
                name: "ai_tip_attempts");

            migrationBuilder.DropIndex(
                name: "ux_tips_member_match",
                table: "tips");

            migrationBuilder.DropIndex(
                name: "ux_tips_user_match",
                table: "tips");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tips_ai_no_joker",
                table: "tips");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tips_owner_xor",
                table: "tips");

            migrationBuilder.DropIndex(
                name: "IX_scored_tips_team_member_id",
                table: "scored_tips");

            migrationBuilder.DropCheckConstraint(
                name: "ck_scored_tips_owner_xor",
                table: "scored_tips");

            migrationBuilder.DropColumn(
                name: "is_ai_fallback",
                table: "tips");

            migrationBuilder.DropColumn(
                name: "reasoning",
                table: "tips");

            migrationBuilder.DropColumn(
                name: "team_member_id",
                table: "tips");

            migrationBuilder.DropColumn(
                name: "team_member_id",
                table: "scored_tips");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "tips",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "scored_tips",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tips_user_id_match_id",
                table: "tips",
                columns: new[] { "user_id", "match_id" },
                unique: true);
        }
    }
}

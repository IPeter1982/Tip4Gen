using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8AdminAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "top_scorer_name",
                table: "tournaments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "winner_team_id",
                table: "tournaments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "admin_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_audit_users_admin_user_id",
                        column: x => x.admin_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tournaments_winner_team_id",
                table: "tournaments",
                column: "winner_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_admin_user_id",
                table: "admin_audit",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_entity_type_entity_id",
                table: "admin_audit",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_occurred_at",
                table: "admin_audit",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_tournaments_teams_national_winner_team_id",
                table: "tournaments",
                column: "winner_team_id",
                principalTable: "teams_national",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tournaments_teams_national_winner_team_id",
                table: "tournaments");

            migrationBuilder.DropTable(
                name: "admin_audit");

            migrationBuilder.DropIndex(
                name: "IX_tournaments_winner_team_id",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "top_scorer_name",
                table: "tournaments");

            migrationBuilder.DropColumn(
                name: "winner_team_id",
                table: "tournaments");
        }
    }
}

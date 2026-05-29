using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notification_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_log", x => x.id);
                    table.CheckConstraint("ck_notification_log_kind", "kind IN ('TipReminder24h','TipReminder2h')");
                    table.ForeignKey(
                        name: "FK_notification_log_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notification_log_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_reminders_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notification_log_dedup",
                table: "notification_log",
                columns: new[] { "user_id", "kind", "match_id" },
                filter: "success = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_notification_log_match_id",
                table: "notification_log",
                column: "match_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notification_log");

            migrationBuilder.DropTable(
                name: "user_preferences");
        }
    }
}

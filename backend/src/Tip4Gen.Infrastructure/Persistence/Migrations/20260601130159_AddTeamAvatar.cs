using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "team_avatar",
                table: "teams",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "team_avatar_content_type",
                table: "teams",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "team_avatar_version",
                table: "teams",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "team_avatar",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "team_avatar_content_type",
                table: "teams");

            migrationBuilder.DropColumn(
                name: "team_avatar_version",
                table: "teams");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tip4Gen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "avatar",
                table: "users",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_content_type",
                table: "users",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_version",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "avatar",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar_content_type",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar_version",
                table: "users");
        }
    }
}

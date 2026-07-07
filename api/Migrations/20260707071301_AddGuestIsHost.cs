using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Picknic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestIsHost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHost",
                table: "Guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHost",
                table: "Guests");
        }
    }
}

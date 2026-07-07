using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Picknic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoDevelopedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DevelopedAt",
                table: "Photos",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DevelopedAt",
                table: "Photos");
        }
    }
}

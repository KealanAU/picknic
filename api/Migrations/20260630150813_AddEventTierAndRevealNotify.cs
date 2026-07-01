using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Picknic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTierAndRevealNotify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevealNotifiedAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "Events",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RevealNotifiedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "Events");
        }
    }
}

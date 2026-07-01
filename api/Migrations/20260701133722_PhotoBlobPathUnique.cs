using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Picknic.Api.Migrations
{
    /// <inheritdoc />
    public partial class PhotoBlobPathUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Photos_BlobPath",
                table: "Photos",
                column: "BlobPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_BlobPath",
                table: "Photos");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScentMatch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddViewHistoryUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_view_history_UserId_PerfumeId",
                table: "view_history",
                columns: new[] { "UserId", "PerfumeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_view_history_UserId_PerfumeId",
                table: "view_history");
        }
    }
}

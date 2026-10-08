using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScentMatch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "brands",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_brands_IsActive",
                table: "brands",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_brands_IsActive",
                table: "brands");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "brands");
        }
    }
}

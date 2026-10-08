using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScentMatch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectionHistoryPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaxPrice",
                table: "selection_history",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinPrice",
                table: "selection_history",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxPrice",
                table: "selection_history");

            migrationBuilder.DropColumn(
                name: "MinPrice",
                table: "selection_history");
        }
    }
}

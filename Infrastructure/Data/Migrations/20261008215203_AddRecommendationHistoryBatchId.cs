using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScentMatch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationHistoryBatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "recommendation_history",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_recommendation_history_UserId_BatchId",
                table: "recommendation_history",
                columns: new[] { "UserId", "BatchId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_recommendation_history_UserId_BatchId",
                table: "recommendation_history");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "recommendation_history");
        }
    }
}

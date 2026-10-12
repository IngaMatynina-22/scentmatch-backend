using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using ScentMatch.Infrastructure.Data;

#nullable disable

namespace ScentMatch.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ScentMatchDbContext))]
    [Migration("20261012190000_ExpandPlatformFeatures")]
    public partial class ExpandPlatformFeatures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "perfumes",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "perfumes",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Intensity",
                table: "perfumes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeMl",
                table: "perfumes",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_perfumes_Barcode",
                table: "perfumes",
                column: "Barcode",
                unique: true);

            migrationBuilder.AddColumn<string>(
                name: "ScenarioKey",
                table: "collections",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_collections_UserId_ScenarioKey",
                table: "collections",
                columns: new[] { "UserId", "ScenarioKey" });

            migrationBuilder.AddColumn<string>(
                name: "ScenarioType",
                table: "selection_history",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Personal")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_selection_history_UserId_ScenarioType",
                table: "selection_history",
                columns: new[] { "UserId", "ScenarioType" });

            migrationBuilder.AddColumn<DateTime>(
                name: "PersonalizationResetAt",
                table: "user_preferences",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseBehavioralSignals",
                table: "user_preferences",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "scent_journal_entries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    TestedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WearMinutes = table.Column<int>(type: "int", nullable: true),
                    OpeningImpression = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeartImpression = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BaseImpression = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TagsJson = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scent_journal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scent_journal_entries_perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scent_journal_entries_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_scent_journal_entries_PerfumeId", table: "scent_journal_entries", column: "PerfumeId");
            migrationBuilder.CreateIndex(name: "IX_scent_journal_entries_Rating", table: "scent_journal_entries", column: "Rating");
            migrationBuilder.CreateIndex(name: "IX_scent_journal_entries_UserId_PerfumeId", table: "scent_journal_entries", columns: new[] { "UserId", "PerfumeId" });
            migrationBuilder.CreateIndex(name: "IX_scent_journal_entries_UserId_TestedOn", table: "scent_journal_entries", columns: new[] { "UserId", "TestedOn" });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wishlist_items_perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wishlist_items_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_wishlist_items_PerfumeId", table: "wishlist_items", column: "PerfumeId");
            migrationBuilder.CreateIndex(name: "IX_wishlist_items_UserId_PerfumeId", table: "wishlist_items", columns: new[] { "UserId", "PerfumeId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_wishlist_items_UserId_Status", table: "wishlist_items", columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateTable(
                name: "price_watches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    ShopLinkId = table.Column<int>(type: "int", nullable: true),
                    SourceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    LastKnownPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastNotifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastNotifiedPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_watches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_watches_perfume_shop_links_ShopLinkId",
                        column: x => x.ShopLinkId,
                        principalTable: "perfume_shop_links",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_price_watches_perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_price_watches_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_price_watches_IsActive_LastCheckedAt", table: "price_watches", columns: new[] { "IsActive", "LastCheckedAt" });
            migrationBuilder.CreateIndex(name: "IX_price_watches_PerfumeId", table: "price_watches", column: "PerfumeId");
            migrationBuilder.CreateIndex(name: "IX_price_watches_ShopLinkId", table: "price_watches", column: "ShopLinkId");
            migrationBuilder.CreateIndex(name: "IX_price_watches_UserId_PerfumeId", table: "price_watches", columns: new[] { "UserId", "PerfumeId" });

            migrationBuilder.CreateTable(
                name: "price_snapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    PriceWatchId = table.Column<int>(type: "int", nullable: true),
                    SourceName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SourceUrl = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_snapshots_perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_price_snapshots_price_watches_PriceWatchId",
                        column: x => x.PriceWatchId,
                        principalTable: "price_watches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_price_snapshots_PerfumeId_CapturedAt", table: "price_snapshots", columns: new[] { "PerfumeId", "CapturedAt" });
            migrationBuilder.CreateIndex(name: "IX_price_snapshots_PriceWatchId", table: "price_snapshots", column: "PriceWatchId");

            migrationBuilder.CreateTable(
                name: "blind_comparison_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WinnerLabel = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blind_comparison_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_blind_comparison_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_blind_comparison_sessions_Status_ExpiresAt", table: "blind_comparison_sessions", columns: new[] { "Status", "ExpiresAt" });
            migrationBuilder.CreateIndex(name: "IX_blind_comparison_sessions_UserId_CreatedAt", table: "blind_comparison_sessions", columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateTable(
                name: "blind_comparison_slots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PerfumeId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blind_comparison_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_blind_comparison_slots_blind_comparison_sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "blind_comparison_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_blind_comparison_slots_perfumes_PerfumeId",
                        column: x => x.PerfumeId,
                        principalTable: "perfumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_blind_comparison_slots_PerfumeId", table: "blind_comparison_slots", column: "PerfumeId");
            migrationBuilder.CreateIndex(name: "IX_blind_comparison_slots_SessionId_Label", table: "blind_comparison_slots", columns: new[] { "SessionId", "Label" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_blind_comparison_slots_SessionId_PerfumeId", table: "blind_comparison_slots", columns: new[] { "SessionId", "PerfumeId" }, unique: true);

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AdminUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Result = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_admin_audit_logs_AdminUserId_CreatedAt", table: "admin_audit_logs", columns: new[] { "AdminUserId", "CreatedAt" });
            migrationBuilder.CreateIndex(name: "IX_admin_audit_logs_EntityType_EntityId", table: "admin_audit_logs", columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateTable(
                name: "background_job_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    JobName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_background_job_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(name: "IX_background_job_logs_JobName_StartedAt", table: "background_job_logs", columns: new[] { "JobName", "StartedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "scent_journal_entries");
            migrationBuilder.DropTable(name: "wishlist_items");
            migrationBuilder.DropTable(name: "price_snapshots");
            migrationBuilder.DropTable(name: "price_watches");
            migrationBuilder.DropTable(name: "blind_comparison_slots");
            migrationBuilder.DropTable(name: "blind_comparison_sessions");
            migrationBuilder.DropTable(name: "admin_audit_logs");
            migrationBuilder.DropTable(name: "background_job_logs");

            migrationBuilder.DropIndex(name: "IX_perfumes_Barcode", table: "perfumes");
            migrationBuilder.DropColumn(name: "Barcode", table: "perfumes");
            migrationBuilder.DropColumn(name: "Currency", table: "perfumes");
            migrationBuilder.DropColumn(name: "Intensity", table: "perfumes");
            migrationBuilder.DropColumn(name: "VolumeMl", table: "perfumes");

            migrationBuilder.DropIndex(name: "IX_collections_UserId_ScenarioKey", table: "collections");
            migrationBuilder.DropColumn(name: "ScenarioKey", table: "collections");

            migrationBuilder.DropIndex(name: "IX_selection_history_UserId_ScenarioType", table: "selection_history");
            migrationBuilder.DropColumn(name: "ScenarioType", table: "selection_history");

            migrationBuilder.DropColumn(name: "PersonalizationResetAt", table: "user_preferences");
            migrationBuilder.DropColumn(name: "UseBehavioralSignals", table: "user_preferences");
        }
    }
}

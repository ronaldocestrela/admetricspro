using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreativeHubAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "CreativeHubAssets",
                schema: "analytics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaFingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    OrganicPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Network = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Permalink = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    EngagementRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BaselineMedianEngagement = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EngagementMultiple = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreativeHubAssets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreativeHubAssets_WorkspaceId_MediaFingerprint",
                schema: "analytics",
                table: "CreativeHubAssets",
                columns: new[] { "WorkspaceId", "MediaFingerprint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreativeHubAssets",
                schema: "analytics");
        }
    }
}

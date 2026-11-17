using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialPublishing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaidBoostSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaidBoostSuggestions",
                schema: "social",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganicPostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    EngagementRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BaselineMedianEngagement = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    EngagementMultiple = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaidBoostSuggestions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaidBoostSuggestions_WorkspaceId_OrganicPostId_Status",
                schema: "social",
                table: "PaidBoostSuggestions",
                columns: new[] { "WorkspaceId", "OrganicPostId", "Status" },
                unique: true,
                filter: "[Status] = 'Open'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaidBoostSuggestions",
                schema: "social");
        }
    }
}

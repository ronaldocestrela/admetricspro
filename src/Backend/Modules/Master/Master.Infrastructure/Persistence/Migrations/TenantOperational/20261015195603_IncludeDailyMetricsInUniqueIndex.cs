using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational
{
    /// <inheritdoc />
    public partial class IncludeDailyMetricsInUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CampaignMetrics_ConnectedAdAccountId_ExternalCampaignId_ExternalAdSetId_ExternalAdId_Date_Hour_Granularity",
                table: "CampaignMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetrics_ConnectedAdAccountId_ExternalCampaignId_ExternalAdSetId_ExternalAdId_Date_Hour_Granularity",
                table: "CampaignMetrics",
                columns: new[] { "ConnectedAdAccountId", "ExternalCampaignId", "ExternalAdSetId", "ExternalAdId", "Date", "Hour", "Granularity" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CampaignMetrics_ConnectedAdAccountId_ExternalCampaignId_ExternalAdSetId_ExternalAdId_Date_Hour_Granularity",
                table: "CampaignMetrics");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetrics_ConnectedAdAccountId_ExternalCampaignId_ExternalAdSetId_ExternalAdId_Date_Hour_Granularity",
                table: "CampaignMetrics",
                columns: new[] { "ConnectedAdAccountId", "ExternalCampaignId", "ExternalAdSetId", "ExternalAdId", "Date", "Hour", "Granularity" },
                unique: true,
                filter: "[ExternalAdSetId] IS NOT NULL AND [ExternalAdId] IS NOT NULL AND [Hour] IS NOT NULL");
        }
    }
}

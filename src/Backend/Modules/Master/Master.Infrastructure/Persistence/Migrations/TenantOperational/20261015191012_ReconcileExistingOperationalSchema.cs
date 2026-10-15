using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>Versiona o schema operacional preexistente necessário à sincronização e auditoria.</summary>
public partial class ReconcileExistingOperationalSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_IsActive",
                table: "TenantUsers");

            migrationBuilder.CreateTable(
                name: "AutomationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConditionTree = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Actions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastTriggeredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedAdAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalCampaignId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DailyBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LifetimeBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "BRL"),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DateRangeStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRangeEndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShareToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShareTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PdfContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ReportDataPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportDispatchLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDispatchLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    ScheduledTimeUtc = table.Column<TimeSpan>(type: "time", nullable: false),
                    DateRangeType = table.Column<int>(type: "int", nullable: false),
                    OutputFormat = table.Column<int>(type: "int", nullable: false),
                    DeliveryChannels = table.Column<int>(type: "int", nullable: false),
                    CustomTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CustomNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IncludeCopilotInsights = table.Column<bool>(type: "bit", nullable: false),
                    IncludeTopCreatives = table.Column<bool>(type: "bit", nullable: false),
                    IncludeChannelBreakdown = table.Column<bool>(type: "bit", nullable: false),
                    IncludePacingSummary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextExecutionAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SafetyGuardIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetEntityName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionTaken = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CurrentSpend = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DailyBudget = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    TargetUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafetyGuardIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedAdAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalAdSetId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BidStrategy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OptimizationGoal = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DailyBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LifetimeBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TargetingSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdSets_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedAdAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExternalCampaignId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ExternalAdSetId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ExternalAdId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: true),
                    Granularity = table.Column<int>(type: "int", nullable: false),
                    Spend = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "BRL"),
                    Impressions = table.Column<long>(type: "bigint", nullable: false),
                    Clicks = table.Column<long>(type: "bigint", nullable: false),
                    Conversions = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConversionValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignMetrics_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectedAdAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalAdId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreativeType = table.Column<int>(type: "int", nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DestinationUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviewUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CallToAction = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ads_AdSets_AdSetId",
                        column: x => x.AdSetId,
                        principalTable: "AdSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Squads_Name",
                table: "Squads",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Ads_AdSetId_ExternalAdId",
                table: "Ads",
                columns: new[] { "AdSetId", "ExternalAdId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdSets_CampaignId_ExternalAdSetId",
                table: "AdSets",
                columns: new[] { "CampaignId", "ExternalAdSetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomationRules_WorkspaceId",
                table: "AutomationRules",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetrics_CampaignId",
                table: "CampaignMetrics",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetrics_ConnectedAdAccountId_ExternalCampaignId_ExternalAdSetId_ExternalAdId_Date_Hour_Granularity",
                table: "CampaignMetrics",
                columns: new[] { "ConnectedAdAccountId", "ExternalCampaignId", "ExternalAdSetId", "ExternalAdId", "Date", "Hour", "Granularity" },
                unique: true,
                filter: "[ExternalAdSetId] IS NOT NULL AND [ExternalAdId] IS NOT NULL AND [Hour] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignMetrics_WorkspaceId",
                table: "CampaignMetrics",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_ConnectedAdAccountId_ExternalCampaignId",
                table: "Campaigns",
                columns: new[] { "ConnectedAdAccountId", "ExternalCampaignId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_WorkspaceId",
                table: "Campaigns",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_GeneratedAtUtc",
                table: "GeneratedReports",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_ShareToken",
                table: "GeneratedReports",
                column: "ShareToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_WorkspaceId",
                table: "GeneratedReports",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDispatchLogs_DispatchedAtUtc",
                table: "ReportDispatchLogs",
                column: "DispatchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDispatchLogs_GeneratedReportId",
                table: "ReportDispatchLogs",
                column: "GeneratedReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_IsActive_NextExecutionAtUtc",
                table: "ReportSchedules",
                columns: new[] { "IsActive", "NextExecutionAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSchedules_WorkspaceId",
                table: "ReportSchedules",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyGuardIncidents_WorkspaceId",
                table: "SafetyGuardIncidents",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SafetyGuardIncidents_WorkspaceId_DetectedAtUtc",
                table: "SafetyGuardIncidents",
                columns: new[] { "WorkspaceId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_Action",
                table: "TenantAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_CreatedAtUtc",
                table: "TenantAuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_Resource_ResourceId",
                table: "TenantAuditLogs",
                columns: new[] { "Resource", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_UserId",
                table: "TenantAuditLogs",
                column: "UserId");
    }
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropTable(
                name: "Ads");

            migrationBuilder.DropTable(
                name: "AutomationRules");

            migrationBuilder.DropTable(
                name: "CampaignMetrics");

            migrationBuilder.DropTable(
                name: "GeneratedReports");

            migrationBuilder.DropTable(
                name: "ReportDispatchLogs");

            migrationBuilder.DropTable(
                name: "ReportSchedules");

            migrationBuilder.DropTable(
                name: "SafetyGuardIncidents");

            migrationBuilder.DropTable(
                name: "TenantAuditLogs");

            migrationBuilder.DropTable(
                name: "AdSets");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Squads_Name",
                table: "Squads");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_IsActive",
                table: "TenantUsers",
                column: "IsActive");
    }
}

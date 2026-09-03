using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migração para inclusão das tabelas ApiQuotaTrackers e TenantApiConnections no Catálogo Master.
/// </summary>
public partial class Add_ApiHealthAndQuotaTracking : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ApiQuotaTrackers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Platform = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                MaxLimit = table.Column<long>(type: "bigint", nullable: false),
                CurrentConsumption = table.Column<long>(type: "bigint", nullable: false),
                WarningThresholdPercentage = table.Column<double>(type: "float", nullable: false),
                CriticalThresholdPercentage = table.Column<double>(type: "float", nullable: false),
                WindowDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                WindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                AlertLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                LastUpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiQuotaTrackers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TenantApiConnections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Platform = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                AccountIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                TokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastSyncAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantApiConnections", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ApiQuotaTrackers_Platform",
            table: "ApiQuotaTrackers",
            column: "Platform",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantApiConnections_TenantId_Platform",
            table: "TenantApiConnections",
            columns: new[] { "TenantId", "Platform" });

        migrationBuilder.CreateIndex(
            name: "IX_TenantApiConnections_Status_Platform",
            table: "TenantApiConnections",
            columns: new[] { "Status", "Platform" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TenantApiConnections");

        migrationBuilder.DropTable(
            name: "ApiQuotaTrackers");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migração para inclusão da tabela MasterAuditLogs no Catálogo Master para auditoria imutável e rastreabilidade de Shadow Mode.
/// </summary>
public partial class Add_MasterAuditLogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MasterAuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Action = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ResourceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                IsImpersonated = table.Column<bool>(type: "bit", nullable: false),
                SuperAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SupportTicketId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ImpersonationSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MasterAuditLogs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MasterAuditLogs_Action",
            table: "MasterAuditLogs",
            column: "Action");

        migrationBuilder.CreateIndex(
            name: "IX_MasterAuditLogs_IsImpersonated_CreatedAtUtc",
            table: "MasterAuditLogs",
            columns: new[] { "IsImpersonated", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_MasterAuditLogs_SuperAdminId_CreatedAtUtc",
            table: "MasterAuditLogs",
            columns: new[] { "SuperAdminId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_MasterAuditLogs_TenantId_CreatedAtUtc",
            table: "MasterAuditLogs",
            columns: new[] { "TenantId", "CreatedAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MasterAuditLogs");
    }
}

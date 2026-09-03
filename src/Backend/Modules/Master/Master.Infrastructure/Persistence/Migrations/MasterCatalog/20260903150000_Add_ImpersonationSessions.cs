using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migration to add ImpersonationSessions table for audited Shadow Mode access.
/// </summary>
public partial class Add_ImpersonationSessions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImpersonationSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SuperAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupportTicketId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RevokeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImpersonationSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_ImpersonationSessions_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ImpersonationSessions_TenantId",
            table: "ImpersonationSessions",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_ImpersonationSessions_SuperAdminId",
            table: "ImpersonationSessions",
            column: "SuperAdminId");

        migrationBuilder.CreateIndex(
            name: "IX_ImpersonationSessions_SupportTicketId",
            table: "ImpersonationSessions",
            column: "SupportTicketId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImpersonationSessions");
    }
}

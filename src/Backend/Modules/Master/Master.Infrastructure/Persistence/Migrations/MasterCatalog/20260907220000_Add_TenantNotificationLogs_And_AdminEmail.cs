using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migration to add TenantNotificationLogs table and AdminEmail/AdminFullName columns to Tenants table.
/// </summary>
public partial class Add_TenantNotificationLogs_And_AdminEmail : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AdminEmail",
            table: "Tenants",
            type: "nvarchar(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AdminFullName",
            table: "Tenants",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "TenantNotificationLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantNotificationLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_TenantNotificationLogs_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TenantNotificationLogs_TenantId_Type",
            table: "TenantNotificationLogs",
            columns: new[] { "TenantId", "Type" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TenantNotificationLogs");

        migrationBuilder.DropColumn(
            name: "AdminFullName",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "AdminEmail",
            table: "Tenants");
    }
}

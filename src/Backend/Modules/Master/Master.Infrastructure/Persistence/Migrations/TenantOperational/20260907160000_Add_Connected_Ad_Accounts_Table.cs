using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>
/// Migração que adiciona a tabela de ConnectedAdAccounts ao schema dos bancos operacionais de inquilinos.
/// </summary>
public partial class Add_Connected_Ad_Accounts_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConnectedAdAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ExternalAccountId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "BRL"),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Connected"),
                IsDemo = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConnectedAdAccounts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConnectedAdAccounts_Workspaces_WorkspaceId",
                    column: x => x.WorkspaceId,
                    principalTable: "Workspaces",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConnectedAdAccounts_WorkspaceId",
            table: "ConnectedAdAccounts",
            column: "WorkspaceId");

        migrationBuilder.CreateIndex(
            name: "IX_ConnectedAdAccounts_WorkspaceId_Platform",
            table: "ConnectedAdAccounts",
            columns: new[] { "WorkspaceId", "Platform" });

        migrationBuilder.CreateIndex(
            name: "IX_ConnectedAdAccounts_IsDemo",
            table: "ConnectedAdAccounts",
            column: "IsDemo");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ConnectedAdAccounts");
    }
}

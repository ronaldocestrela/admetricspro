using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>Adiciona states OAuth de uso único e vínculos seguros de contas por workspace.</summary>
public partial class SecureMetaWorkspaceConnections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.AddColumn<string>(
                name: "AuthorizedTasks",
                table: "ConnectedAdAccounts",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ExternalAccountStatus",
                table: "ConnectedAdAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAtUtc",
                table: "ConnectedAdAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OAuthTokenVaultId",
                table: "ConnectedAdAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "ConnectedAdAccounts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_OAuthTokenVaults_WorkspaceId_Id",
                table: "OAuthTokenVaults",
                columns: new[] { "WorkspaceId", "Id" });

            migrationBuilder.CreateTable(
                name: "OAuthAuthorizationStates",
                columns: table => new
                {
                    StateHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuthAuthorizationStates", x => x.StateHash);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedAdAccounts_WorkspaceId_OAuthTokenVaultId",
                table: "ConnectedAdAccounts",
                columns: new[] { "WorkspaceId", "OAuthTokenVaultId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedAdAccounts_WorkspaceId_Platform_ExternalAccountId",
                table: "ConnectedAdAccounts",
                columns: new[] { "WorkspaceId", "Platform", "ExternalAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuthAuthorizationStates_ExpiresAtUtc",
                table: "OAuthAuthorizationStates",
                column: "ExpiresAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_ConnectedAdAccounts_OAuthTokenVaults_WorkspaceId_OAuthTokenVaultId",
                table: "ConnectedAdAccounts",
                columns: new[] { "WorkspaceId", "OAuthTokenVaultId" },
                principalTable: "OAuthTokenVaults",
                principalColumns: new[] { "WorkspaceId", "Id" },
                onDelete: ReferentialAction.Restrict);
    }
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.DropForeignKey(
                name: "FK_ConnectedAdAccounts_OAuthTokenVaults_WorkspaceId_OAuthTokenVaultId",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropTable(
                name: "OAuthAuthorizationStates");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_OAuthTokenVaults_WorkspaceId_Id",
                table: "OAuthTokenVaults");

            migrationBuilder.DropIndex(
                name: "IX_ConnectedAdAccounts_WorkspaceId_OAuthTokenVaultId",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ConnectedAdAccounts_WorkspaceId_Platform_ExternalAccountId",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropColumn(
                name: "AuthorizedTasks",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropColumn(
                name: "ExternalAccountStatus",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropColumn(
                name: "LastSyncedAtUtc",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropColumn(
                name: "OAuthTokenVaultId",
                table: "ConnectedAdAccounts");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "ConnectedAdAccounts");
    }
}

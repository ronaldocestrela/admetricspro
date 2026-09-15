using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>
/// Migração que provisiona a tabela OAuthTokenVaults para armazenamento cifrado em repouso de credenciais OAuth2 no banco do tenant.
/// </summary>
public partial class Add_OAuthTokenVaults_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OAuthTokenVaults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ExternalAccountId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                ExternalAccountName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                EncryptedAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                EncryptedRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                AccessTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RefreshTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Active"),
                Scopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OAuthTokenVaults", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OAuthTokenVaults_WorkspaceId_Platform",
            table: "OAuthTokenVaults",
            columns: new[] { "WorkspaceId", "Platform" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "OAuthTokenVaults");
    }
}

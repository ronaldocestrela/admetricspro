using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>
/// Adiciona a tabela de convites de colaboradores ao schema operacional do inquilino.
/// </summary>
public partial class Add_TenantUserInvitations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TenantUserInvitations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantUserInvitations", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TenantUserInvitations_TokenHash",
            table: "TenantUserInvitations",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TenantUserInvitations_UserId",
            table: "TenantUserInvitations",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_TenantUserInvitations_UserId_AcceptedAtUtc_RevokedAtUtc",
            table: "TenantUserInvitations",
            columns: new[] { "UserId", "AcceptedAtUtc", "RevokedAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TenantUserInvitations");
    }
}
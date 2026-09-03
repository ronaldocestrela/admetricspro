using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Initial migration for the master catalog database, creating the Tenants table and constraints.
/// </summary>
public partial class Initial_MasterCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Cnpj = table.Column<string>(type: "nvarchar(14)", maxLength: 14, nullable: false),
                Subdomain = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                EncryptedConnectionString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Tier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                SubscriptionExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Cnpj",
            table: "Tenants",
            column: "Cnpj",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Subdomain",
            table: "Tenants",
            column: "Subdomain",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Tenants");
    }
}

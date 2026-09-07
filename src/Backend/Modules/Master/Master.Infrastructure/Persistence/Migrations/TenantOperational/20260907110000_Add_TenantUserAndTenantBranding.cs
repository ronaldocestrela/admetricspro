using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>
/// Migration adding TenantUsers and TenantBranding tables to the operational tenant database schema.
/// </summary>
public partial class Add_TenantUserAndTenantBranding : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TenantUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TenantBranding",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PrimaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                SecondaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                LightLogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                DarkLogoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                FaviconUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TenantBranding", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TenantUsers_Email",
            table: "TenantUsers",
            column: "Email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TenantBranding");

        migrationBuilder.DropTable(
            name: "TenantUsers");
    }
}

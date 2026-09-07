using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.TenantOperational;

/// <summary>
/// Migration adding the Workspaces table to the operational tenant database schema.
/// </summary>
public partial class Add_Workspaces_Table : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Workspaces",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                CnpjOrCpf = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                MonthlyAdSpendBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Segment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Workspaces", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Workspaces_CnpjOrCpf",
            table: "Workspaces",
            column: "CnpjOrCpf");

        migrationBuilder.CreateIndex(
            name: "IX_Workspaces_IsActive",
            table: "Workspaces",
            column: "IsActive");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Workspaces");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migration to add the SubscriptionPlans table in the master catalog.
/// </summary>
public partial class Add_SubscriptionPlans : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SubscriptionPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Tier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                AnnualDiscountPercentage = table.Column<int>(type: "int", nullable: false),
                MaxSeats = table.Column<int>(type: "int", nullable: false),
                MaxWorkspaces = table.Column<int>(type: "int", nullable: false),
                MonthlyAdSpendCap = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                HasWhiteLabel = table.Column<bool>(type: "bit", nullable: false),
                HasCustomCname = table.Column<bool>(type: "bit", nullable: false),
                HasAiCopilot = table.Column<bool>(type: "bit", nullable: false),
                HasCrossNetworkAutomations = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SubscriptionPlans_Name",
            table: "SubscriptionPlans",
            column: "Name",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SubscriptionPlans");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migration to add onboarding profile and white-label branding columns to Tenants table.
/// </summary>
public partial class Add_TenantOnboardingProfileAndBranding : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BillingCycle",
            table: "Tenants",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CustomDomain",
            table: "Tenants",
            type: "nvarchar(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MonthlyAdSpendRange",
            table: "Tenants",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PrimaryColor",
            table: "Tenants",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SecondaryColor",
            table: "Tenants",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Segment",
            table: "Tenants",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "BillingCycle",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "CustomDomain",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "MonthlyAdSpendRange",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "PrimaryColor",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "SecondaryColor",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "Segment",
            table: "Tenants");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migration to add DunningStage and PaymentDueDateUtc columns to Tenants table.
/// </summary>
public partial class Add_TenantDunningStage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DunningStage",
            table: "Tenants",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "None");

        migrationBuilder.AddColumn<DateTime>(
            name: "PaymentDueDateUtc",
            table: "Tenants",
            type: "datetime2",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DunningStage",
            table: "Tenants");

        migrationBuilder.DropColumn(
            name: "PaymentDueDateUtc",
            table: "Tenants");
    }
}

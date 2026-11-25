using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Master.Domain.FeatureFlags;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <inheritdoc />
public partial class Add_MessagingDispatchKillSwitch : Migration
{
    private static readonly Guid FlagId = new("b0000002-0000-0000-0000-000000000009");

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var seedTime = new DateTime(2026, 11, 25, 21, 0, 0, DateTimeKind.Utc);

        migrationBuilder.InsertData(
            table: "FeatureFlags",
            columns:
            [
                "Id", "Key", "Name", "Description", "IsEnabled", "IsKillSwitch", "TargetingType",
                "RolloutPercentage", "TargetTenantIds", "CreatedBy", "CreatedAtUtc", "UpdatedAtUtc",
            ],
            values:
            [
                FlagId,
                MasterCatalogFeatureFlagKeys.MessagingDispatch,
                "Kill Switch — Despacho de Outbox (RabbitMQ)",
                "Congela o OutboxDispatchWorker e impede publicação no broker.",
                false,
                true,
                "Global",
                100,
                "[]",
                "system@admetricspro.com",
                seedTime,
                seedTime,
            ]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "FeatureFlags",
            keyColumn: "Id",
            keyValue: FlagId);
    }
}
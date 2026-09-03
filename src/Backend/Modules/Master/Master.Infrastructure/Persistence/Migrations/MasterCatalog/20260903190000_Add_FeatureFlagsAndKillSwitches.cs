using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Master.Infrastructure.Persistence.Migrations.MasterCatalog;

/// <summary>
/// Migração para inclusão da tabela FeatureFlags e seed inicial de Kill Switches operacionais.
/// </summary>
public partial class Add_FeatureFlagsAndKillSwitches : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeatureFlags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                IsKillSwitch = table.Column<bool>(type: "bit", nullable: false),
                TargetingType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                RolloutPercentage = table.Column<int>(type: "int", nullable: false),
                TargetTenantIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                KillSwitchActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                KillSwitchReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                KillSwitchTriggeredBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeatureFlags", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFlags_Key",
            table: "FeatureFlags",
            column: "Key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFlags_IsKillSwitch",
            table: "FeatureFlags",
            column: "IsKillSwitch");

        migrationBuilder.CreateIndex(
            name: "IX_FeatureFlags_IsKillSwitch_IsEnabled",
            table: "FeatureFlags",
            columns: new[] { "IsKillSwitch", "IsEnabled" });

        // Seed Standard Kill Switches and Feature Flags
        var seedTime = new DateTime(2026, 9, 3, 19, 0, 0, DateTimeKind.Utc);

        migrationBuilder.InsertData(
            table: "FeatureFlags",
            columns: new[] { "Id", "Key", "Name", "Description", "IsEnabled", "IsKillSwitch", "TargetingType", "RolloutPercentage", "TargetTenantIds", "CreatedBy", "CreatedAtUtc", "UpdatedAtUtc" },
            values: new object[,]
            {
                {
                    new Guid("b0000001-0000-0000-0000-000000000001"),
                    "killswitch.automation.global",
                    "Kill Switch Global de Automações Cross-Network",
                    "Disjuntor operacional que congela instantaneamente a execução de todas as regras de automação em todas as redes de anúncios.",
                    false,
                    true,
                    "Global",
                    100,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                },
                {
                    new Guid("b0000001-0000-0000-0000-000000000002"),
                    "killswitch.automation.meta",
                    "Kill Switch de Automação — Meta Ads",
                    "Congela exclusivamente as automações e regras de corte de gastos conectadas à Meta Graph API.",
                    false,
                    true,
                    "Global",
                    100,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                },
                {
                    new Guid("b0000001-0000-0000-0000-000000000003"),
                    "killswitch.automation.google",
                    "Kill Switch de Automação — Google Ads",
                    "Congela exclusivamente as automações e regras de corte de gastos conectadas ao Google Ads API.",
                    false,
                    true,
                    "Global",
                    100,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                },
                {
                    new Guid("b0000001-0000-0000-0000-000000000004"),
                    "killswitch.automation.tiktok",
                    "Kill Switch de Automação — TikTok Ads",
                    "Congela exclusivamente as automações e regras de corte de gastos conectadas ao TikTok Marketing API.",
                    false,
                    true,
                    "Global",
                    100,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                },
                {
                    new Guid("b0000001-0000-0000-0000-000000000005"),
                    "killswitch.automation.bing",
                    "Kill Switch de Automação — Bing Ads",
                    "Congela exclusivamente as automações e regras de corte de gastos conectadas ao Microsoft Advertising (Bing).",
                    false,
                    true,
                    "Global",
                    100,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                },
                {
                    new Guid("b0000001-0000-0000-0000-000000000006"),
                    "killswitch.data-sync.global",
                    "Kill Switch Global de Sincronização em Background",
                    "Congela o agendador de ingestão e sincronização de métricas e conversões em segundo plano.",
                    false,
                    true,
                    "Global",
                    100,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                },
                {
                    new Guid("b0000001-0000-0000-0000-000000000007"),
                    "feature.analytics.mer-v2",
                    "Motor de Atribuição e MER v2",
                    "Novo algoritmo avançado de Marketing Efficiency Ratio com deduplicação de conversões cross-channel.",
                    true,
                    false,
                    "PercentageRollout",
                    20,
                    "[]",
                    "system@admetricspro.com",
                    seedTime,
                    seedTime
                }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "FeatureFlags");
    }
}

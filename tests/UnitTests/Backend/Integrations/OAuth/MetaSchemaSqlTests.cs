using FluentAssertions;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Valida DDL SQL Server sem depender de Docker ou conexão externa.</summary>
public sealed class MetaSchemaSqlTests
{
    /// <summary>O caminho de upgrade deve criar estados/vínculos e o diário, sem drift do modelo.</summary>
    [Fact]
    public void MigrationScript_ContainsRequiredObjectsAndMatchesCurrentModel()
    {
        using var context = new TenantOperationalDbContext(new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlServer("Server=localhost;Database=DesignOnly;Trusted_Connection=True").Options);
        var sql = context.GetService<IMigrator>().GenerateScript("20260915180000_Add_OAuthTokenVaults_Table", options: MigrationsSqlGenerationOptions.Idempotent);
        sql.Should().Contain("ADD [OAuthTokenVaultId]").And.Contain("CREATE TABLE [OAuthAuthorizationStates]")
            .And.Contain("CREATE TABLE [MetaApiOperations]").And.Contain("FK_ConnectedAdAccounts_OAuthTokenVaults_WorkspaceId_OAuthTokenVaultId");
        context.Database.HasPendingModelChanges().Should().BeFalse();
    }
    /// <summary>O índice idempotente também deve abranger métricas diárias com dimensões nulas.</summary>
    [Fact]
    public void DailyMetricUniqueIndex_MustIncludeNullDimensions()
    {
        using var context = new TenantOperationalDbContext(new DbContextOptionsBuilder<TenantOperationalDbContext>()
            .UseSqlServer("Server=localhost;Database=DesignOnly;Trusted_Connection=True").Options);
        var script = context.Database.GenerateCreateScript();
        var index = script.Split('\n').Single(line => line.StartsWith("CREATE UNIQUE INDEX [IX_CampaignMetrics_", StringComparison.Ordinal));
        index.Should().NotContain("WHERE", "métricas em nível de campanha/dia têm dimensões nulas e também precisam de unicidade");
    }
}

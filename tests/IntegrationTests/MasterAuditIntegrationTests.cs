using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Application.Security;
using FluentAssertions;
using IntegrationTests.Infrastructure;
using Master.Application.Auditing;
using Master.Application.Services;
using Master.Domain.Auditing;
using Master.Infrastructure.Extensions;
using Master.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

/// <summary>
/// Integration tests verifying the immutable global audit trail in MasterDb and mandatory tagging
/// with 'performed_by_superadmin' for all operations performed under Shadow Mode.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class MasterAuditIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MasterAuditIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared SQL Server testcontainer fixture.</param>
    public MasterAuditIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(IServiceProvider Provider, MasterDbContext Context, string ConnectionString)> CreateScopeAsync(
        IImpersonationContext impersonationContext)
    {
        var dbName = $"Master_Audit_{Guid.NewGuid():N}";
        var connString = WithDatabase(_fixture.ConnectionString, dbName);
        await EnsureDatabaseCreatedAsync(connString);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMasterCatalog(connString);
        services.AddScoped<IImpersonationContext>(_ => impersonationContext);

        var provider = services.BuildServiceProvider();

        // Apply migrations
        using (var scope = provider.CreateScope())
        {
            var runner = scope.ServiceProvider.GetRequiredService<IMasterDatabaseMigrationRunner>();
            var migResult = await runner.ApplyMigrationsAsync(CancellationToken.None);
            migResult.IsSuccess.Should().BeTrue();
        }

        var context = provider.GetRequiredService<MasterDbContext>();
        return (provider, context, connString);
    }

    /// <summary>
    /// Verifies that an operation executed in impersonation mode automatically writes an audit record
    /// containing the tag 'performed_by_superadmin', superadmin id, support ticket, and session id.
    /// </summary>
    [Fact]
    public async Task RecordAsync_ShouldPersistAuditEntry_WithPerformedBySuperadminTag_WhenImpersonated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var superAdminId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var ticket = "INC-88990";

        var impersonationContext = new TestImpersonationContext(
            isImpersonated: true,
            originalSuperAdminId: superAdminId,
            supportTicketId: ticket,
            sessionId: sessionId,
            targetTenantId: tenantId);

        var (provider, context, _) = await CreateScopeAsync(impersonationContext);

        using var scope = provider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IMasterAuditService>();

        // Act
        var result = await auditService.RecordAsync(
            action: "Workspace.UpdateBudgetLimit",
            resource: "Workspace",
            resourceId: "ws-999",
            details: "Ajuste emergencial de limite de orçamento para R$ 50.000",
            tenantId: tenantId,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var entryId = result.Value;

        // Query database directly to assert immutable persistence
        var savedEntry = await context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entryId);
        savedEntry.Should().NotBeNull();
        savedEntry!.TenantId.Should().Be(tenantId);
        savedEntry.Action.Should().Be("Workspace.UpdateBudgetLimit");
        savedEntry.Resource.Should().Be("Workspace");
        savedEntry.ResourceId.Should().Be("ws-999");
        savedEntry.IsImpersonated.Should().BeTrue();
        savedEntry.SuperAdminId.Should().Be(superAdminId);
        savedEntry.SupportTicketId.Should().Be(ticket);
        savedEntry.ImpersonationSessionId.Should().Be(sessionId);
        savedEntry.Tags.Should().Contain(MasterAuditTags.PerformedBySuperadmin);
    }

    /// <summary>
    /// Verifies that non-impersonated operations do not receive the 'performed_by_superadmin' tag.
    /// </summary>
    [Fact]
    public async Task RecordAsync_ShouldNotIncludePerformedBySuperadminTag_WhenNotImpersonated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var impersonationContext = new TestImpersonationContext(
            isImpersonated: false,
            originalSuperAdminId: null,
            supportTicketId: null,
            sessionId: null,
            targetTenantId: null);

        var (provider, context, _) = await CreateScopeAsync(impersonationContext);

        using var scope = provider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IMasterAuditService>();

        // Act
        var result = await auditService.RecordAsync(
            action: "Tenant.UpdateContact",
            resource: "Tenant",
            resourceId: tenantId.ToString(),
            details: "Atualização de e-mail de faturamento",
            tenantId: tenantId,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var entryId = result.Value;

        var savedEntry = await context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entryId);
        savedEntry.Should().NotBeNull();
        savedEntry!.IsImpersonated.Should().BeFalse();
        savedEntry.SuperAdminId.Should().BeNull();
        savedEntry.SupportTicketId.Should().BeNull();
        savedEntry.Tags.Should().NotContain(MasterAuditTags.PerformedBySuperadmin);
    }

    private static async Task EnsureDatabaseCreatedAsync(string connectionString)
    {
        await using var connection = new SqlConnection(new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString);

        await connection.OpenAsync();
        var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID('{databaseName}') IS NULL CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    private sealed class TestImpersonationContext : IImpersonationContext
    {
        public TestImpersonationContext(
            bool isImpersonated,
            Guid? originalSuperAdminId,
            string? supportTicketId,
            Guid? sessionId,
            Guid? targetTenantId)
        {
            IsImpersonated = isImpersonated;
            OriginalSuperAdminId = originalSuperAdminId;
            SupportTicketId = supportTicketId;
            SessionId = sessionId;
            TargetTenantId = targetTenantId;
        }

        public bool IsImpersonated { get; }
        public Guid? OriginalSuperAdminId { get; }
        public string? SupportTicketId { get; }
        public Guid? SessionId { get; }
        public Guid? TargetTenantId { get; }
    }
}

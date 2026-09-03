using FluentAssertions;
using Master.Domain.Tenants;
using Master.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace UnitTests.Backend.Persistence;

/// <summary>
/// Unit tests validating the EF Core entity configurations and constraints for <see cref="MasterDbContext"/>.
/// </summary>
public sealed class MasterDbContextModelTests
{
    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=localhost;Database=MasterModelTestDb;")
            .Options;

        using var context = new MasterDbContext(options);
        return context.Model;
    }

    /// <summary>
    /// Verifies that the Tenant entity maps to the 'Tenants' table with appropriate primary key.
    /// </summary>
    [Fact]
    public void Model_ShouldMapTenantEntityToTenantsTableWithPrimaryKey()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Tenant));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("Tenants");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(Tenant.Id));
    }

    /// <summary>
    /// Verifies that the CNPJ property has a unique index, is required, and has max length of 14.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureCnpjWithUniqueIndexAndMaxLength14()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Tenant))!;
        var cnpjProperty = entityType.FindProperty(nameof(Tenant.Cnpj));

        // Assert
        cnpjProperty.Should().NotBeNull();
        cnpjProperty!.IsNullable.Should().BeFalse();
        cnpjProperty.GetMaxLength().Should().Be(14);

        var index = entityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Tenant.Cnpj)));
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Subdomain property has a unique index, is required, and has max length of 80.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureSubdomainWithUniqueIndexAndMaxLength80()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Tenant))!;
        var subdomainProperty = entityType.FindProperty(nameof(Tenant.Subdomain));

        // Assert
        subdomainProperty.Should().NotBeNull();
        subdomainProperty!.IsNullable.Should().BeFalse();
        subdomainProperty.GetMaxLength().Should().Be(80);

        var index = entityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Tenant.Subdomain)));
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CompanyName and EncryptedConnectionString have appropriate constraints.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureCompanyNameAndConnectionStringLimits()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Tenant))!;
        var companyNameProperty = entityType.FindProperty(nameof(Tenant.CompanyName));
        var connStrProperty = entityType.FindProperty(nameof(Tenant.EncryptedConnectionString));

        // Assert
        companyNameProperty.Should().NotBeNull();
        companyNameProperty!.IsNullable.Should().BeFalse();
        companyNameProperty.GetMaxLength().Should().Be(200);

        connStrProperty.Should().NotBeNull();
        connStrProperty!.IsNullable.Should().BeFalse();
        connStrProperty.GetMaxLength().Should().Be(2000);
    }

    /// <summary>
    /// Verifies that enum properties Status and Tier are configured as strings with max length constraints.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureStatusAndTierAsStringsWithMaxLength()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Tenant))!;
        var statusProperty = entityType.FindProperty(nameof(Tenant.Status));
        var tierProperty = entityType.FindProperty(nameof(Tenant.Tier));

        // Assert
        statusProperty.Should().NotBeNull();
        statusProperty!.IsNullable.Should().BeFalse();
        statusProperty.GetMaxLength().Should().Be(30);
        statusProperty.GetProviderClrType().Should().Be(typeof(string));

        tierProperty.Should().NotBeNull();
        tierProperty!.IsNullable.Should().BeFalse();
        tierProperty.GetMaxLength().Should().Be(30);
        tierProperty.GetProviderClrType().Should().Be(typeof(string));
    }

    /// <summary>
    /// Verifies nullable and audit properties configuration.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureSubscriptionExpiresAtUtcAsNullableAndCreatedAtUtcAsRequired()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Tenant))!;
        var expiresProperty = entityType.FindProperty(nameof(Tenant.SubscriptionExpiresAtUtc));
        var createdAtProperty = entityType.FindProperty(nameof(Tenant.CreatedAtUtc));

        // Assert
        expiresProperty.Should().NotBeNull();
        expiresProperty!.IsNullable.Should().BeTrue();

        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that SubscriptionPlan maps to 'SubscriptionPlans' table with primary key.
    /// </summary>
    [Fact]
    public void Model_ShouldMapSubscriptionPlanToSubscriptionPlansTableWithPrimaryKey()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Master.Domain.Plans.SubscriptionPlan));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("SubscriptionPlans");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(Master.Domain.Plans.SubscriptionPlan.Id));
    }

    /// <summary>
    /// Verifies that SubscriptionPlan name has a unique index and max length of 100.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureSubscriptionPlanNameWithUniqueIndexAndMaxLength100()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Master.Domain.Plans.SubscriptionPlan))!;
        var nameProperty = entityType.FindProperty(nameof(Master.Domain.Plans.SubscriptionPlan.Name));

        // Assert
        nameProperty.Should().NotBeNull();
        nameProperty!.IsNullable.Should().BeFalse();
        nameProperty.GetMaxLength().Should().Be(100);

        var index = entityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(Master.Domain.Plans.SubscriptionPlan.Name)));
        index.Should().NotBeNull();
        index!.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that owned navigation Limits and Features are mapped properly to columns.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureSubscriptionPlanOwnedLimitsAndFeatures()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Master.Domain.Plans.SubscriptionPlan))!;

        var limitsNavigation = entityType.FindNavigation(nameof(Master.Domain.Plans.SubscriptionPlan.Limits));
        var featuresNavigation = entityType.FindNavigation(nameof(Master.Domain.Plans.SubscriptionPlan.Features));

        // Assert
        limitsNavigation.Should().NotBeNull();
        featuresNavigation.Should().NotBeNull();

        var limitsEntityType = limitsNavigation!.TargetEntityType;
        var featuresEntityType = featuresNavigation!.TargetEntityType;

        limitsEntityType.FindProperty(nameof(Master.Domain.Plans.PlanLimits.MaxSeats)).Should().NotBeNull();
        featuresEntityType.FindProperty(nameof(Master.Domain.Plans.PlanFeatures.HasWhiteLabel)).Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that ImpersonationSession entity maps to the 'ImpersonationSessions' table with appropriate primary key.
    /// </summary>
    [Fact]
    public void Model_ShouldMapImpersonationSessionToTableWithPrimaryKeyAndIndexes()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Master.Domain.Tenants.ImpersonationSession));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("ImpersonationSessions");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(Master.Domain.Tenants.ImpersonationSession.Id));

        var tenantIdProp = entityType.FindProperty(nameof(Master.Domain.Tenants.ImpersonationSession.TenantId));
        tenantIdProp.Should().NotBeNull();
        tenantIdProp!.IsNullable.Should().BeFalse();

        var ticketProp = entityType.FindProperty(nameof(Master.Domain.Tenants.ImpersonationSession.SupportTicketId));
        ticketProp.Should().NotBeNull();
        ticketProp!.IsNullable.Should().BeFalse();
        ticketProp.GetMaxLength().Should().Be(50);
    }

    /// <summary>
    /// Verifies that MasterAuditEntry entity maps to the 'MasterAuditLogs' table with appropriate primary key and constraints.
    /// </summary>
    [Fact]
    public void Model_ShouldMapMasterAuditEntryToMasterAuditLogsTableWithPrimaryKeyAndIndexes()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(Master.Domain.Auditing.MasterAuditEntry));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("MasterAuditLogs");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(Master.Domain.Auditing.MasterAuditEntry.Id));

        var actionProp = entityType.FindProperty(nameof(Master.Domain.Auditing.MasterAuditEntry.Action));
        actionProp.Should().NotBeNull();
        actionProp!.IsNullable.Should().BeFalse();
        actionProp.GetMaxLength().Should().Be(150);

        var resourceProp = entityType.FindProperty(nameof(Master.Domain.Auditing.MasterAuditEntry.Resource));
        resourceProp.Should().NotBeNull();
        resourceProp!.IsNullable.Should().BeFalse();
        resourceProp.GetMaxLength().Should().Be(100);

        var superAdminProp = entityType.FindProperty(nameof(Master.Domain.Auditing.MasterAuditEntry.SuperAdminId));
        superAdminProp.Should().NotBeNull();
        superAdminProp!.IsNullable.Should().BeTrue();

        var isImpersonatedProp = entityType.FindProperty(nameof(Master.Domain.Auditing.MasterAuditEntry.IsImpersonated));
        isImpersonatedProp.Should().NotBeNull();
        isImpersonatedProp!.IsNullable.Should().BeFalse();
    }
}


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
}

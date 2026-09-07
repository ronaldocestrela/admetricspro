using BuildingBlocks.Domain.Tenants;
using BuildingBlocks.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace UnitTests.Backend.Persistence;

/// <summary>
/// Unit tests validating the EF Core entity configurations and constraints for <see cref="TenantDbContext"/>.
/// </summary>
public sealed class TenantDbContextModelTests
{
    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlServer("Server=localhost;Database=TenantModelTestDb;")
            .Options;

        using var context = new TenantDbContext(options);
        return context.Model;
    }

    /// <summary>
    /// Verifies that the TenantUser entity maps to 'TenantUsers' table with appropriate primary key.
    /// </summary>
    [Fact]
    public void Model_ShouldMapTenantUserEntityToTenantUsersTableWithPrimaryKey()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(TenantUser));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("TenantUsers");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(TenantUser.Id));
    }

    /// <summary>
    /// Verifies that the TenantUser properties have expected column constraints and unique index on Email.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureTenantUserPropertiesAndUniqueEmailIndex()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(TenantUser))!;

        // FullName
        var nameProp = entityType.FindProperty(nameof(TenantUser.FullName));
        nameProp.Should().NotBeNull();
        nameProp!.IsNullable.Should().BeFalse();
        nameProp.GetMaxLength().Should().Be(200);

        // Email
        var emailProp = entityType.FindProperty(nameof(TenantUser.Email));
        emailProp.Should().NotBeNull();
        emailProp!.IsNullable.Should().BeFalse();
        emailProp.GetMaxLength().Should().Be(256);

        // Unique index on Email
        var emailIndex = entityType.GetIndexes().FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(TenantUser.Email)));
        emailIndex.Should().NotBeNull();
        emailIndex!.IsUnique.Should().BeTrue();

        // PhoneNumber
        var phoneProp = entityType.FindProperty(nameof(TenantUser.PhoneNumber));
        phoneProp.Should().NotBeNull();
        phoneProp!.IsNullable.Should().BeTrue();
        phoneProp.GetMaxLength().Should().Be(50);

        // PasswordHash
        var hashProp = entityType.FindProperty(nameof(TenantUser.PasswordHash));
        hashProp.Should().NotBeNull();
        hashProp!.IsNullable.Should().BeFalse();
        hashProp.GetMaxLength().Should().Be(500);

        // Role
        var roleProp = entityType.FindProperty(nameof(TenantUser.Role));
        roleProp.Should().NotBeNull();
        roleProp!.IsNullable.Should().BeFalse();

        // IsActive
        var activeProp = entityType.FindProperty(nameof(TenantUser.IsActive));
        activeProp.Should().NotBeNull();
        activeProp!.IsNullable.Should().BeFalse();

        // CreatedAtUtc
        var createdProp = entityType.FindProperty(nameof(TenantUser.CreatedAtUtc));
        createdProp.Should().NotBeNull();
        createdProp!.IsNullable.Should().BeFalse();

        // UpdatedAtUtc
        var updatedProp = entityType.FindProperty(nameof(TenantUser.UpdatedAtUtc));
        updatedProp.Should().NotBeNull();
        updatedProp!.IsNullable.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the TenantBranding entity maps to 'TenantBranding' table with appropriate primary key.
    /// </summary>
    [Fact]
    public void Model_ShouldMapTenantBrandingEntityToTenantBrandingTableWithPrimaryKey()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(TenantBranding));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("TenantBranding");

        var primaryKey = entityType.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().ContainSingle(p => p.Name == nameof(TenantBranding.Id));
    }

    /// <summary>
    /// Verifies that the TenantBranding properties have expected column constraints.
    /// </summary>
    [Fact]
    public void Model_ShouldConfigureTenantBrandingProperties()
    {
        // Arrange
        var model = CreateModel();
        var entityType = model.FindEntityType(typeof(TenantBranding))!;

        // PrimaryColor
        var primaryProp = entityType.FindProperty(nameof(TenantBranding.PrimaryColor));
        primaryProp.Should().NotBeNull();
        primaryProp!.IsNullable.Should().BeFalse();
        primaryProp.GetMaxLength().Should().Be(9);

        // SecondaryColor
        var secondaryProp = entityType.FindProperty(nameof(TenantBranding.SecondaryColor));
        secondaryProp.Should().NotBeNull();
        secondaryProp!.IsNullable.Should().BeFalse();
        secondaryProp.GetMaxLength().Should().Be(9);

        // LightLogoUrl
        var lightLogoProp = entityType.FindProperty(nameof(TenantBranding.LightLogoUrl));
        lightLogoProp.Should().NotBeNull();
        lightLogoProp!.IsNullable.Should().BeTrue();
        lightLogoProp.GetMaxLength().Should().Be(1000);

        // DarkLogoUrl
        var darkLogoProp = entityType.FindProperty(nameof(TenantBranding.DarkLogoUrl));
        darkLogoProp.Should().NotBeNull();
        darkLogoProp!.IsNullable.Should().BeTrue();
        darkLogoProp.GetMaxLength().Should().Be(1000);

        // FaviconUrl
        var faviconProp = entityType.FindProperty(nameof(TenantBranding.FaviconUrl));
        faviconProp.Should().NotBeNull();
        faviconProp!.IsNullable.Should().BeTrue();
        faviconProp.GetMaxLength().Should().Be(1000);

        // CreatedAtUtc
        var createdProp = entityType.FindProperty(nameof(TenantBranding.CreatedAtUtc));
        createdProp.Should().NotBeNull();
        createdProp!.IsNullable.Should().BeFalse();

        // UpdatedAtUtc
        var updatedProp = entityType.FindProperty(nameof(TenantBranding.UpdatedAtUtc));
        updatedProp.Should().NotBeNull();
        updatedProp!.IsNullable.Should().BeTrue();
    }
}

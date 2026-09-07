using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="TenantUser"/> entity and lifecycle invariants.
/// </summary>
public sealed class TenantUserTests
{
    private readonly Guid _validId = Guid.NewGuid();
    private const string ValidFullName = "Carlos Mendes";
    private const string ValidEmail = "carlos@vanguarda.com.br";
    private const string ValidPhone = "11987654321";
    private const string ValidPasswordHash = "AQAAAAEAACcQAAAAEHASH1234567890SECUREHASH";

    /// <summary>
    /// Verifies that a valid TenantUser can be instantiated via factory method.
    /// </summary>
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Act
        var result = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var user = result.Value;
        user.Id.Should().Be(_validId);
        user.FullName.Should().Be(ValidFullName);
        user.Email.Should().Be("carlos@vanguarda.com.br");
        user.PhoneNumber.Should().Be(ValidPhone);
        user.PasswordHash.Should().Be(ValidPasswordHash);
        user.Role.Should().Be(TenantRole.Owner);
        user.IsActive.Should().BeTrue();
        user.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        user.UpdatedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Verifies that creating a TenantUser with an empty Guid returns a validation error.
    /// </summary>
    [Fact]
    public void Create_WithEmptyId_ShouldReturnFailure()
    {
        // Act
        var result = TenantUser.Create(
            Guid.Empty,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.InvalidId");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with empty or whitespace full name returns a validation error.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidFullName_ShouldReturnFailure(string? invalidFullName)
    {
        // Act
        var result = TenantUser.Create(
            _validId,
            invalidFullName!,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.InvalidFullName");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with full name exceeding 200 characters returns a validation error.
    /// </summary>
    [Fact]
    public void Create_WithFullNameTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longName = new string('A', 201);

        // Act
        var result = TenantUser.Create(
            _validId,
            longName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.FullNameTooLong");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with an invalid email address returns a validation error.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("carlos@")]
    [InlineData("@domain.com")]
    public void Create_WithInvalidEmail_ShouldReturnFailure(string? invalidEmail)
    {
        // Act
        var result = TenantUser.Create(
            _validId,
            ValidFullName,
            invalidEmail!,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.InvalidEmail");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with email exceeding 256 characters returns a validation error.
    /// </summary>
    [Fact]
    public void Create_WithEmailTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longEmail = $"{new string('a', 250)}@test.com";

        // Act
        var result = TenantUser.Create(
            _validId,
            ValidFullName,
            longEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.EmailTooLong");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with phone number exceeding 50 characters returns a validation error.
    /// </summary>
    [Fact]
    public void Create_WithPhoneNumberTooLong_ShouldReturnFailure()
    {
        // Arrange
        var longPhone = new string('1', 51);

        // Act
        var result = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            longPhone,
            ValidPasswordHash,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.PhoneNumberTooLong");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with empty password hash returns a validation error.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPasswordHash_ShouldReturnFailure(string? invalidHash)
    {
        // Act
        var result = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            invalidHash!,
            TenantRole.Owner);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.InvalidPasswordHash");
    }

    /// <summary>
    /// Verifies that creating a TenantUser with undefined role returns a validation error.
    /// </summary>
    [Fact]
    public void Create_WithUndefinedRole_ShouldReturnFailure()
    {
        // Act
        var result = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            (TenantRole)999);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantUser.InvalidRole");
    }

    /// <summary>
    /// Verifies that profile update modifies full name and phone number and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateFieldsAndTimestamp()
    {
        // Arrange
        var user = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Admin).Value;

        // Act
        var updateResult = user.UpdateProfile("Carlos Silva Mendes", "11999998888");

        // Assert
        updateResult.IsSuccess.Should().BeTrue();
        user.FullName.Should().Be("Carlos Silva Mendes");
        user.PhoneNumber.Should().Be("11999998888");
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that password hash change updates the hash and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void ChangePasswordHash_WithValidHash_ShouldUpdateHashAndTimestamp()
    {
        // Arrange
        var user = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Admin).Value;

        const string newHash = "NEW_SECURE_HASH_VAL_987654";

        // Act
        var result = user.ChangePasswordHash(newHash);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be(newHash);
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that changing the role updates the role and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void ChangeRole_WithValidRole_ShouldUpdateRoleAndTimestamp()
    {
        // Arrange
        var user = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Analyst).Value;

        // Act
        var result = user.ChangeRole(TenantRole.MediaManager);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Role.Should().Be(TenantRole.MediaManager);
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that deactivating an active user sets IsActive to false and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void Deactivate_ActiveUser_ShouldSetIsActiveFalse()
    {
        // Arrange
        var user = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Admin).Value;

        // Act
        var result = user.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that activating an inactive user sets IsActive to true and sets UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void Activate_InactiveUser_ShouldSetIsActiveTrue()
    {
        // Arrange
        var user = TenantUser.Create(
            _validId,
            ValidFullName,
            ValidEmail,
            ValidPhone,
            ValidPasswordHash,
            TenantRole.Admin).Value;
        user.Deactivate();

        // Act
        var result = user.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.UpdatedAtUtc.Should().NotBeNull();
    }
}

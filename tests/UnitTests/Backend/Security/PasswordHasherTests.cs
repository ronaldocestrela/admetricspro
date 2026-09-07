using BuildingBlocks.Application.Security;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;

namespace UnitTests.Backend.Security;

/// <summary>
/// Unit tests for <see cref="PasswordHasher"/> validating cryptographic hashing, salting, and verification.
/// </summary>
public sealed class PasswordHasherTests
{
    private readonly IPasswordHasher _sut = new PasswordHasher();

    /// <summary>
    /// Verifies that HashPassword returns a non-empty, non-plaintext hash string.
    /// </summary>
    [Fact]
    public void HashPassword_ShouldReturnSecureHash_WhenPasswordIsValid()
    {
        // Arrange
        const string password = "StrongPassword@2026!";

        // Act
        var hash = _sut.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(password);
        hash.Length.Should().BeGreaterThan(20);
    }

    /// <summary>
    /// Verifies that hashing the same password twice yields distinct hashes due to cryptographic salting.
    /// </summary>
    [Fact]
    public void HashPassword_ShouldGenerateDistinctHashes_ForSamePasswordDueToSalt()
    {
        // Arrange
        const string password = "ConsistentPassword123#";

        // Act
        var hash1 = _sut.HashPassword(password);
        var hash2 = _sut.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    /// <summary>
    /// Verifies that HashPassword throws an ArgumentException when password is null, empty, or whitespace.
    /// </summary>
    /// <param name="invalidPassword">Invalid password sample.</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsInvalid(string? invalidPassword)
    {
        // Act
        var act = () => _sut.HashPassword(invalidPassword!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that VerifyPassword returns true when the correct password matches the hash.
    /// </summary>
    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        // Arrange
        const string password = "SecretPassword$456";
        var hash = _sut.HashPassword(password);

        // Act
        var isValid = _sut.VerifyPassword(hash, password);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that VerifyPassword returns false when an incorrect password is provided.
    /// </summary>
    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
    {
        // Arrange
        const string correctPassword = "CorrectPassword#1";
        const string wrongPassword = "WrongPassword#2";
        var hash = _sut.HashPassword(correctPassword);

        // Act
        var isValid = _sut.VerifyPassword(hash, wrongPassword);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that VerifyPassword returns false when given invalid hash or password inputs.
    /// </summary>
    /// <param name="hash">Candidate hash string.</param>
    /// <param name="password">Candidate password string.</param>
    [Theory]
    [InlineData(null, "some-password")]
    [InlineData("", "some-password")]
    [InlineData("   ", "some-password")]
    [InlineData("not-a-valid-hash", "some-password")]
    [InlineData("AQAAAAIAAYagAAAA", null)]
    [InlineData("AQAAAAIAAYagAAAA", "")]
    [InlineData("AQAAAAIAAYagAAAA", "   ")]
    public void VerifyPassword_ShouldReturnFalse_WhenInputsAreInvalid(string? hash, string? password)
    {
        // Act
        var isValid = _sut.VerifyPassword(hash!, password!);

        // Assert
        isValid.Should().BeFalse();
    }
}

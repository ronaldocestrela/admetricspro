using System.Security.Cryptography;
using BuildingBlocks.Infrastructure.Security;
using FluentAssertions;

namespace UnitTests.Backend.Security;

/// <summary>
/// Unit tests for <see cref="AesEncryptionService"/>.
/// </summary>
public sealed class AesEncryptionServiceTests
{
    private static readonly string Valid32ByteKey = Convert.ToBase64String(new byte[32]
    {
        1, 2, 3, 4, 5, 6, 7, 8,
        9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24,
        25, 26, 27, 28, 29, 30, 31, 32
    });

    /// <summary>
    /// Verifies constructor throws when key is not 32 bytes.
    /// </summary>
    [Theory]
    [InlineData(16)] // 128-bit key
    [InlineData(24)] // 192-bit key
    [InlineData(48)] // 384-bit key
    public void Constructor_ShouldThrowArgumentException_WhenKeyLengthIsNot32Bytes(int keySizeInBytes)
    {
        // Arrange
        var invalidKey = Convert.ToBase64String(new byte[keySizeInBytes]);

        // Act
        var act = () => new AesEncryptionService(invalidKey);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*AES-256 key must be 32 bytes*");
    }

    /// <summary>
    /// Verifies constructor throws when key is invalid Base64.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowFormatException_WhenKeyIsNotBase64()
    {
        // Act
        var act = () => new AesEncryptionService("not-a-valid-base64-string!!");

        // Assert
        act.Should().Throw<FormatException>();
    }

    /// <summary>
    /// Verifies encrypt and decrypt restores original plaintext.
    /// </summary>
    [Theory]
    [InlineData("Server=tcp:sql.local,1433;Database=Tenant_01;User Id=sa;Password=Secret123!;")]
    [InlineData("CustomSpecialChars: !@#$%&*()_+-=[]{}|;':,.<>?/`~")]
    [InlineData("A very long connection string with lots of parameters and details to ensure multiple blocks are encrypted correctly without truncation or padding issues.")]
    public void Encrypt_And_Decrypt_ShouldPreserveExactPlainText(string plainText)
    {
        // Arrange
        var service = new AesEncryptionService(Valid32ByteKey);

        // Act
        var cipherText = service.Encrypt(plainText);
        var decrypted = service.Decrypt(cipherText);

        // Assert
        cipherText.Should().NotBeNullOrWhiteSpace();
        cipherText.Should().NotBe(plainText);
        decrypted.Should().Be(plainText);
    }

    /// <summary>
    /// Verifies encrypting same plaintext twice generates different ciphertexts due to random IV.
    /// </summary>
    [Fact]
    public void Encrypt_SamePlainTextTwice_ShouldGenerateDifferentCipherTexts()
    {
        // Arrange
        var service = new AesEncryptionService(Valid32ByteKey);
        const string plainText = "Server=tcp:sql.local;Database=Tenant_01;";

        // Act
        var cipher1 = service.Encrypt(plainText);
        var cipher2 = service.Encrypt(plainText);

        // Assert
        cipher1.Should().NotBe(cipher2);
        service.Decrypt(cipher1).Should().Be(plainText);
        service.Decrypt(cipher2).Should().Be(plainText);
    }

    /// <summary>
    /// Verifies encrypt throws on null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Encrypt_ShouldThrowArgumentException_WhenPlainTextIsNullOrWhitespace(string? invalidInput)
    {
        // Arrange
        var service = new AesEncryptionService(Valid32ByteKey);

        // Act
        var act = () => service.Encrypt(invalidInput!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies decrypt throws on null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decrypt_ShouldThrowArgumentException_WhenCipherTextIsNullOrWhitespace(string? invalidInput)
    {
        // Arrange
        var service = new AesEncryptionService(Valid32ByteKey);

        // Act
        var act = () => service.Decrypt(invalidInput!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies decrypt throws CryptographicException when payload is too short (smaller than IV).
    /// </summary>
    [Fact]
    public void Decrypt_ShouldThrowCryptographicException_WhenPayloadIsTooShort()
    {
        // Arrange
        var service = new AesEncryptionService(Valid32ByteKey);
        var shortPayload = Convert.ToBase64String(new byte[8]); // less than 16 bytes IV

        // Act
        var act = () => service.Decrypt(shortPayload);

        // Assert
        act.Should().Throw<CryptographicException>()
            .WithMessage("*Invalid encrypted payload*");
    }

    /// <summary>
    /// Verifies decrypt throws CryptographicException when ciphertext bytes are tampered with.
    /// </summary>
    [Fact]
    public void Decrypt_ShouldThrowCryptographicException_WhenCipherTextIsTampered()
    {
        // Arrange
        var service = new AesEncryptionService(Valid32ByteKey);
        var cipherText = service.Encrypt("Valid connection string");
        var rawBytes = Convert.FromBase64String(cipherText);

        // Tamper with the last byte
        rawBytes[^1] = (byte)(rawBytes[^1] ^ 0xFF);
        var tamperedCipherText = Convert.ToBase64String(rawBytes);

        // Act
        var act = () => service.Decrypt(tamperedCipherText);

        // Assert
        act.Should().Throw<CryptographicException>();
    }
}

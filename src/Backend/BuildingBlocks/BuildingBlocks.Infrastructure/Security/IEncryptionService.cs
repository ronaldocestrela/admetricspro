namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Provides symmetric encryption capabilities for sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plain text into a base64 payload.
    /// </summary>
    /// <param name="plainText">Plain text value to encrypt.</param>
    /// <returns>Encrypted base64 payload.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a base64 payload into plain text.
    /// </summary>
    /// <param name="cipherText">Encrypted base64 payload.</param>
    /// <returns>Decrypted plain text.</returns>
    string Decrypt(string cipherText);
}
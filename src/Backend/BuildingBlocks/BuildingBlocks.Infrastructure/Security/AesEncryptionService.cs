using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Encrypts and decrypts values using AES-256-CBC with random IV.
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private const int IvSizeBytes = 16;
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesEncryptionService"/> class.
    /// </summary>
    /// <param name="base64Key">Base64-encoded 32-byte key.</param>
    public AesEncryptionService(string base64Key)
    {
        var keyBytes = Convert.FromBase64String(base64Key);
        if (keyBytes.Length != 32)
        {
            throw new ArgumentException("AES-256 key must be 32 bytes.", nameof(base64Key));
        }

        _key = keyBytes;
    }

    /// <inheritdoc />
    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var payload = new byte[IvSizeBytes + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, IvSizeBytes);
        Buffer.BlockCopy(cipherBytes, 0, payload, IvSizeBytes, cipherBytes.Length);

        return Convert.ToBase64String(payload);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        var payload = Convert.FromBase64String(cipherText);

        if (payload.Length <= IvSizeBytes)
        {
            throw new CryptographicException("Invalid encrypted payload.");
        }

        var iv = new byte[IvSizeBytes];
        var cipherBytes = new byte[payload.Length - IvSizeBytes];
        Buffer.BlockCopy(payload, 0, iv, 0, IvSizeBytes);
        Buffer.BlockCopy(payload, IvSizeBytes, cipherBytes, 0, cipherBytes.Length);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
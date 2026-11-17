using System.Security.Cryptography;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Security;
using SocialPublishing.Application.Abstractions;
using SocialPublishing.Domain;

namespace SocialPublishing.Infrastructure.Security;

/// <summary>
/// Cifra segredos OAuth orgânicos com envelope JSON contendo propósito autenticado (AES-256-CBC do kernel).
/// </summary>
public sealed class OrganicTokenProtector : IOrganicTokenProtector
{
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Inicializa o protetor de tokens orgânicos.
    /// </summary>
    /// <param name="encryptionService">Serviço simétrico do kernel.</param>
    public OrganicTokenProtector(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <inheritdoc />
    public string Protect(string plainSecret, string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var envelope = JsonSerializer.Serialize(new TokenEnvelope(purpose, plainSecret));
        return _encryptionService.Encrypt(envelope);
    }

    /// <inheritdoc />
    public Result<string> Unprotect(string cipherText, string expectedPurpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPurpose);

        try
        {
            var json = _encryptionService.Decrypt(cipherText);
            var envelope = JsonSerializer.Deserialize<TokenEnvelope>(json);
            if (envelope is null ||
                !string.Equals(envelope.Purpose, expectedPurpose, StringComparison.Ordinal))
            {
                return Result<string>.Failure(SocialErrors.VaultPurposeMismatch);
            }

            return Result<string>.Success(envelope.Secret);
        }
        catch (CryptographicException)
        {
            return Result<string>.Failure(SocialErrors.VaultPurposeMismatch);
        }
        catch (JsonException)
        {
            return Result<string>.Failure(SocialErrors.VaultPurposeMismatch);
        }
    }

    private sealed record TokenEnvelope(string Purpose, string Secret);
}

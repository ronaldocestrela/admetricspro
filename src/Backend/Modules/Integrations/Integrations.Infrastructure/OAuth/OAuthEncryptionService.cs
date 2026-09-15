using BuildingBlocks.Infrastructure.Security;
using Integrations.Domain.OAuth;

namespace Integrations.Infrastructure.OAuth;

/// <summary>
/// Implementação de <see cref="IOAuthEncryptionService"/> que delega para o <see cref="IEncryptionService"/>
/// baseado em AES-256 com IV aleatório por operação.
/// </summary>
public sealed class OAuthEncryptionService : IOAuthEncryptionService
{
    private readonly IEncryptionService _encryptionService;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthEncryptionService"/>.
    /// </summary>
    /// <param name="encryptionService">Serviço simétrico AES-256 da infraestrutura.</param>
    public OAuthEncryptionService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    /// <inheritdoc />
    public string Encrypt(string plainText) => _encryptionService.Encrypt(plainText);

    /// <inheritdoc />
    public string Decrypt(string cipherText) => _encryptionService.Decrypt(cipherText);
}

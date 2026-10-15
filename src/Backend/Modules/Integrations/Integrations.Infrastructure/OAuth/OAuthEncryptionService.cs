using BuildingBlocks.Infrastructure.Security;
using Integrations.Domain.OAuth;
using Integrations.Application.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Integrations.Infrastructure.OAuth;

/// <summary>
/// Implementação de <see cref="IOAuthEncryptionService"/> que delega para o <see cref="IEncryptionService"/>
/// baseado em AES-256 com IV aleatório por operação.
/// </summary>
public sealed class OAuthEncryptionService : IOAuthEncryptionService
{
    private readonly IEncryptionService _encryptionService;
    private readonly OAuthNetworkOptions _options;
    private readonly BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor? _tenants;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthEncryptionService"/>.
    /// </summary>
    /// <param name="encryptionService">Serviço simétrico AES-256 da infraestrutura.</param>
    /// <param name="options">Chave externa do cofre OAuth.</param>
    /// <param name="tenants">Tenant do escopo autenticado ou do job.</param>
    public OAuthEncryptionService(IEncryptionService encryptionService, IOptions<OAuthNetworkOptions> options, BuildingBlocks.Application.MultiTenancy.ITenantContextAccessor? tenants = null)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _options = options.Value;
        _tenants = tenants;
    }

    /// <inheritdoc />
    public string Encrypt(string plainText, string? purpose = null)
    {
        // Preserva provedores legados quando a Meta não está habilitada. O startup exige
        // chave GCM externa sempre que um App ID Meta está configurado.
        if (string.IsNullOrWhiteSpace(_options.TokenEncryptionKey)) return _encryptionService.Encrypt(plainText);
        var key = Convert.FromBase64String(_options.TokenEncryptionKey);
        using var aes = new AesGcm(key, 16);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        aes.Encrypt(nonce, plain, cipher, tag, AssociatedData(purpose));
        CryptographicOperations.ZeroMemory(plain);
        return "gcm1." + Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText, string? purpose = null)
    {
        // Compatibilidade de leitura com o cofre anterior; novas gravações sempre usam AES-GCM.
        if (!cipherText.StartsWith("gcm1.", StringComparison.Ordinal))
        {
            if (purpose is not null) throw new CryptographicException("Reconecte a credencial legada para vinculá-la ao contexto autenticado.");
            return _encryptionService.Decrypt(cipherText);
        }
        var bytes = Convert.FromBase64String(cipherText[5..]);
        if (bytes.Length < 28) throw new CryptographicException("Credencial cifrada inválida.");
        using var aes = new AesGcm(Convert.FromBase64String(_options.TokenEncryptionKey), 16);
        var plain = new byte[bytes.Length - 28];
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), plain, AssociatedData(purpose));
        var value = Encoding.UTF8.GetString(plain);
        CryptographicOperations.ZeroMemory(plain);
        return value;
    }

    private byte[] AssociatedData(string? purpose) => Encoding.UTF8.GetBytes($"AdMetricsPro:OAuth:{_tenants?.TenantContext?.TenantId}:{purpose}");
}

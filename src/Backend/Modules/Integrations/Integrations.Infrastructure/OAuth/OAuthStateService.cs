using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Domain.OAuth;

namespace Integrations.Infrastructure.OAuth;

/// <summary>
/// Implementação de <see cref="IOAuthStateService"/> que empacota o estado OAuth em JSON Base64Url
/// protegido por assinatura criptográfica HMAC-SHA256 e controle de expiração temporal (TTL de 15 minutos).
/// </summary>
public sealed class OAuthStateService : IOAuthStateService
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(15);
    private readonly byte[] _signingKey;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="OAuthStateService"/>.
    /// </summary>
    /// <param name="signingKey">Chave secreta de assinatura HMAC.</param>
    public OAuthStateService(string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new ArgumentException("A chave de assinatura de estado OAuth não pode ser vazia.", nameof(signingKey));
        }

        _signingKey = Encoding.UTF8.GetBytes(signingKey);
    }

    /// <inheritdoc />
    public string GenerateState(Guid tenantId, Guid workspaceId, string platform, string redirectUri)
    {
        return GenerateStateWithTimestamp(tenantId, workspaceId, platform, redirectUri, DateTime.UtcNow);
    }

    /// <summary>
    /// Método interno para facilitar testes com timestamps pré-fixados.
    /// </summary>
    internal string GenerateStateWithTimestamp(
        Guid tenantId,
        Guid workspaceId,
        string platform,
        string redirectUri,
        DateTime createdAtUtc)
    {
        var payload = new OAuthStatePayload(tenantId, workspaceId, platform, redirectUri, createdAtUtc);
        var json = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(json);
        var payloadBase64 = Convert.ToBase64String(payloadBytes);

        using var hmac = new HMACSHA256(_signingKey);
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
        var signatureBase64 = Convert.ToBase64String(signatureBytes);

        return $"{payloadBase64}.{signatureBase64}";
    }

    /// <inheritdoc />
    public Result<OAuthStatePayload> ValidateAndUnpackState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return Result<OAuthStatePayload>.Failure(
                Error.Validation("OAuthState.InvalidFormat", "O parâmetro state recebido é nulo ou vazio."));
        }

        var parts = state.Split('.');
        if (parts.Length != 2)
        {
            return Result<OAuthStatePayload>.Failure(
                Error.Validation("OAuthState.InvalidFormat", "O formato do parâmetro state é inválido."));
        }

        var payloadBase64 = parts[0];
        var signatureBase64 = parts[1];

        using var hmac = new HMACSHA256(_signingKey);
        var expectedSignatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadBase64));
        var expectedSignatureBase64 = Convert.ToBase64String(expectedSignatureBytes);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(signatureBase64),
                Encoding.UTF8.GetBytes(expectedSignatureBase64)))
        {
            return Result<OAuthStatePayload>.Failure(
                Error.Validation("OAuthState.InvalidSignature", "A assinatura criptográfica do estado OAuth é inválida ou foi adulterada."));
        }

        OAuthStatePayload? payload;
        try
        {
            var payloadBytes = Convert.FromBase64String(payloadBase64);
            var json = Encoding.UTF8.GetString(payloadBytes);
            payload = JsonSerializer.Deserialize<OAuthStatePayload>(json);
        }
        catch
        {
            return Result<OAuthStatePayload>.Failure(
                Error.Validation("OAuthState.InvalidFormat", "Não foi possível deserializar o payload do estado OAuth."));
        }

        if (payload is null)
        {
            return Result<OAuthStatePayload>.Failure(
                Error.Validation("OAuthState.InvalidFormat", "O payload de estado é nulo."));
        }

        if (DateTime.UtcNow - payload.CreatedAtUtc > StateLifetime)
        {
            return Result<OAuthStatePayload>.Failure(
                Error.Validation("OAuthState.Expired", "A requisição de autorização OAuth expirou por limite temporal (15 minutos)."));
        }

        return Result<OAuthStatePayload>.Success(payload);
    }
}

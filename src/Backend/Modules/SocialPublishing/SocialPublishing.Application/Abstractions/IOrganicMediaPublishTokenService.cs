using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Emite e valida tokens HMAC de leitura pública de mídia orgânica.
/// </summary>
public interface IOrganicMediaPublishTokenService
{
    /// <summary>
    /// Gera um token opaco assinado para leitura pública do objeto.
    /// </summary>
    /// <param name="tenantId">Tenant dono da chave.</param>
    /// <param name="objectKey">Chave no bucket orgânico.</param>
    /// <param name="contentType">Tipo MIME gravado no ativo.</param>
    /// <param name="utcNow">Instante de emissão (injetável em testes).</param>
    /// <returns>Token no formato <c>payload.signature</c> Base64Url.</returns>
    Result<string> IssueToken(Guid tenantId, string objectKey, string contentType, DateTime utcNow);

    /// <summary>
    /// Valida assinatura, expiração e coerência tenant × chave.
    /// </summary>
    /// <param name="token">Token recebido na rota pública.</param>
    /// <param name="utcNow">Relógio de validação.</param>
    /// <returns>Payload validado ou falha opaca para o cliente.</returns>
    Result<OrganicMediaPublishTokenPayload> ValidateToken(string token, DateTime utcNow);
}

/// <summary>
/// Conteúdo assinado de um link público de mídia orgânica.
/// </summary>
/// <param name="TenantId">Tenant dono do objeto.</param>
/// <param name="ObjectKey">Chave no bucket orgânico.</param>
/// <param name="ContentType">Tipo MIME para o cabeçalho HTTP.</param>
/// <param name="ExpiresAtUtc">Expiração absoluta em UTC.</param>
public sealed record OrganicMediaPublishTokenPayload(
    Guid TenantId,
    string ObjectKey,
    string ContentType,
    DateTime ExpiresAtUtc);
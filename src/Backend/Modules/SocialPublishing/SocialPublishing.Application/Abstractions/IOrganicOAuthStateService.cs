using BuildingBlocks.Domain.Primitives;
using SocialPublishing.Domain.Connections;

namespace SocialPublishing.Application.Abstractions;

/// <summary>
/// Contrato para emissão e validação de tokens de state OAuth orgânico anti-CSRF.
/// </summary>
public interface IOrganicOAuthStateService
{
    /// <summary>
    /// Emite um state assinado com HMAC-SHA256 e TTL de 15 minutos a partir do instante informado.
    /// </summary>
    /// <param name="tenantId">Identificador do inquilino.</param>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma orgânica alvo.</param>
    /// <param name="redirectUri">URI de retorno autorizada para o fluxo.</param>
    /// <param name="issuedAtUtc">Instante UTC de emissão (determinístico em testes).</param>
    /// <returns>State codificado e instante de expiração UTC.</returns>
    OrganicOAuthStateIssueResult Issue(
        Guid tenantId,
        Guid workspaceId,
        OrganicPlatform platform,
        string redirectUri,
        DateTime issuedAtUtc);

    /// <summary>
    /// Valida integridade, expiração e correspondência de tenant/workspace do state recebido.
    /// </summary>
    /// <param name="state">String de state recebida no callback ou na verificação.</param>
    /// <param name="expectedTenantId">Tenant esperado no contexto atual.</param>
    /// <param name="expectedWorkspaceId">Workspace esperado no contexto atual.</param>
    /// <param name="utcNow">Relógio UTC de referência para TTL e detecção de emissão futura.</param>
    /// <returns>Payload deserializado ou <see cref="SocialPublishing.Domain.SocialErrors.OAuthStateInvalid"/>.</returns>
    Result<OrganicOAuthStatePayload> Validate(
        string state,
        Guid expectedTenantId,
        Guid expectedWorkspaceId,
        DateTime utcNow);
}

/// <summary>
/// Resultado da emissão de state OAuth orgânico.
/// </summary>
/// <param name="State">Token de state assinado enviado ao provedor.</param>
/// <param name="ExpiresAtUtc">Instante UTC em que o state deixa de ser válido.</param>
public sealed record OrganicOAuthStateIssueResult(string State, DateTime ExpiresAtUtc);

/// <summary>
/// Dados empacotados no state OAuth orgânico após validação bem-sucedida.
/// </summary>
/// <param name="TenantId">Inquilino que iniciou o fluxo.</param>
/// <param name="WorkspaceId">Workspace alvo da conexão.</param>
/// <param name="Platform">Rede orgânica solicitada.</param>
/// <param name="RedirectUri">URI de retorno acordada na emissão.</param>
/// <param name="IssuedAtUtc">Carimbo UTC de emissão do state.</param>
/// <param name="Nonce">Entropia exclusiva do fluxo (não é token de acesso).</param>
public sealed record OrganicOAuthStatePayload(
    Guid TenantId,
    Guid WorkspaceId,
    OrganicPlatform Platform,
    string RedirectUri,
    DateTime IssuedAtUtc,
    string Nonce);

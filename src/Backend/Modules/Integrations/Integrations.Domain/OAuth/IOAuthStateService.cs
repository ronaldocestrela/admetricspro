using BuildingBlocks.Domain.Primitives;

namespace Integrations.Domain.OAuth;

/// <summary>
/// Contrato do serviço para geração e validação de tokens de estado OAuth anti-CSRF.
/// </summary>
public interface IOAuthStateService
{
    /// <summary>
    /// Gera um token de estado assinado e protegido contra CSRF contendo o contexto da requisição.
    /// </summary>
    /// <param name="tenantId">Identificador do inquilino.</param>
    /// <param name="workspaceId">Identificador do workspace.</param>
    /// <param name="platform">Plataforma alvo.</param>
    /// <param name="redirectUri">URI de redirecionamento de retorno.</param>
    /// <param name="userId">Usuário autenticado que iniciou a autorização.</param>
    /// <returns>String codificada em Base64 contendo payload e assinatura HMAC.</returns>
    string GenerateState(Guid tenantId, Guid workspaceId, string platform, string redirectUri, Guid userId = default);

    /// <summary>
    /// Valida a assinatura, verifica a janela de expiração e extrai o payload original do estado.
    /// </summary>
    /// <param name="state">String de estado recebida no callback.</param>
    /// <returns>Resultado contendo os dados empacotados ou falha de validação/adulteração.</returns>
    Result<OAuthStatePayload> ValidateAndUnpackState(string state);
}

/// <summary>
/// Estrutura de dados preservada dentro do estado de autorização OAuth.
/// </summary>
/// <param name="TenantId">Identificador do inquilino contextual.</param>
/// <param name="WorkspaceId">Identificador do workspace associado à conexão.</param>
/// <param name="Platform">Nome da plataforma de anúncios.</param>
/// <param name="RedirectUri">URI de redirecionamento autorizada.</param>
/// <param name="CreatedAtUtc">Data/hora UTC de criação do estado para controle de TTL.</param>
/// <param name="UserId">Usuário que iniciou o fluxo.</param>
/// <param name="Nonce">Valor aleatório exclusivo do fluxo.</param>
public sealed record OAuthStatePayload(
    Guid TenantId,
    Guid WorkspaceId,
    string Platform,
    string RedirectUri,
    DateTime CreatedAtUtc,
    Guid UserId = default,
    string Nonce = "");

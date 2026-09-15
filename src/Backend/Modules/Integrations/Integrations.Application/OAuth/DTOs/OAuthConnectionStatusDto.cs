namespace Integrations.Application.OAuth.DTOs;

/// <summary>
/// DTO seguro representando o status operacional e metadados de uma conexão OAuth sem expor tokens confidenciais.
/// </summary>
/// <param name="Id">Identificador do registro no Token Vault.</param>
/// <param name="WorkspaceId">Identificador do workspace.</param>
/// <param name="Platform">Plataforma de anúncios.</param>
/// <param name="ExternalAccountId">ID externo da conta na rede.</param>
/// <param name="ExternalAccountName">Nome amigável da conta.</param>
/// <param name="Status">Status operacional (Active, Expired, Revoked).</param>
/// <param name="Scopes">Escopos concedidos.</param>
/// <param name="AccessTokenExpiresAtUtc">Data de expiração do access token.</param>
/// <param name="IsExpiringSoon">Indica se o token está a menos de 72 horas da expiração.</param>
/// <param name="CreatedAtUtc">Data de criação da conexão.</param>
/// <param name="UpdatedAtUtc">Data da última atualização.</param>
public sealed record OAuthConnectionStatusDto(
    Guid Id,
    Guid WorkspaceId,
    string Platform,
    string ExternalAccountId,
    string ExternalAccountName,
    string Status,
    string Scopes,
    DateTime? AccessTokenExpiresAtUtc,
    bool IsExpiringSoon,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

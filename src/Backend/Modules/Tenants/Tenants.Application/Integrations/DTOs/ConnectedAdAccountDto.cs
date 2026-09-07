namespace Tenants.Application.Integrations.DTOs;

/// <summary>
/// Objeto de transferência com as informações cadastrais de uma conta de anúncios vinculada.
/// </summary>
/// <param name="Id">Identificador único da conta conectada.</param>
/// <param name="WorkspaceId">Identificador do workspace associado.</param>
/// <param name="Platform">Plataforma de anúncios (ex: MetaAds, GoogleAds).</param>
/// <param name="ExternalAccountId">ID externo da conta na rede.</param>
/// <param name="AccountName">Nome amigável da conta de anúncios.</param>
/// <param name="Currency">Moeda da conta.</param>
/// <param name="Status">Status operacional da conexão.</param>
/// <param name="IsDemo">Indica se é uma conta em modo demonstração.</param>
/// <param name="CreatedAtUtc">Data de vinculação.</param>
public sealed record ConnectedAdAccountDto(
    Guid Id,
    Guid WorkspaceId,
    string Platform,
    string ExternalAccountId,
    string AccountName,
    string Currency,
    string Status,
    bool IsDemo,
    DateTime CreatedAtUtc);

namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Representa os estados de ciclo de vida universais de uma campanha de anúncios.
/// </summary>
public enum CampaignStatus
{
    /// <summary>
    /// Estado desconhecido ou não identificado.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Campanha ativa e veiculando impressões.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Campanha pausada pelo usuário ou por regra automatizada.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Campanha arquivada ou finalizada na rede externa.
    /// </summary>
    Archived = 3,

    /// <summary>
    /// Campanha excluída na rede externa.
    /// </summary>
    Deleted = 4
}

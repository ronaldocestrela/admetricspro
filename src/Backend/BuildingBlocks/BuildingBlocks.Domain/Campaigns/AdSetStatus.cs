namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Representa os estados de ciclo de vida universais de um conjunto ou grupo de anúncios (AdSet / AdGroup).
/// </summary>
public enum AdSetStatus
{
    /// <summary>
    /// Estado desconhecido ou não identificado.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Conjunto ativo e elegível para entrega.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Conjunto pausado.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Conjunto arquivado.
    /// </summary>
    Archived = 3,

    /// <summary>
    /// Conjunto excluído.
    /// </summary>
    Deleted = 4
}

namespace BuildingBlocks.Domain.Campaigns;

/// <summary>
/// Representa os estados operacionais e de aprovação de um anúncio (Ad / Creative).
/// </summary>
public enum AdStatus
{
    /// <summary>
    /// Estado desconhecido ou não identificado.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Anúncio aprovado e ativo para veiculação.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Anúncio pausado.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Anúncio arquivado.
    /// </summary>
    Archived = 3,

    /// <summary>
    /// Anúncio excluído na plataforma de origem.
    /// </summary>
    Deleted = 4,

    /// <summary>
    /// Anúncio reprovado pela política da rede de anúncios.
    /// </summary>
    Disapproved = 5
}

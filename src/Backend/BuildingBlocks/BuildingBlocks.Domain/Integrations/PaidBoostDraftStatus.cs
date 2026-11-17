namespace BuildingBlocks.Domain.Integrations;

/// <summary>
/// Status operacional de um rascunho local de impulsionamento a partir de tração orgânica.
/// </summary>
public enum PaidBoostDraftStatus
{
    /// <summary>
    /// Rascunho pausado aguardando revisão humana antes de qualquer escrita na rede de anúncios.
    /// </summary>
    Paused = 1,
}

namespace SocialPublishing.Domain.Traction;

/// <summary>
/// Status do ciclo de vida de uma sugestão de impulsionamento pago a partir de tração orgânica.
/// </summary>
public enum PaidBoostSuggestionStatus
{
    /// <summary>
    /// Sugestão aguardando decisão do usuário.
    /// </summary>
    Open = 1,

    /// <summary>
    /// Usuário aceitou e um rascunho de anúncio foi solicitado.
    /// </summary>
    Accepted = 2,

    /// <summary>
    /// Usuário dispensou a sugestão.
    /// </summary>
    Dismissed = 3,
}

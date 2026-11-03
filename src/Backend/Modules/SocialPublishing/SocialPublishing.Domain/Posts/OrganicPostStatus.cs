namespace SocialPublishing.Domain.Posts;

/// <summary>
/// Status do ciclo de vida de um post orgânico agendado.
/// </summary>
public enum OrganicPostStatus
{
    /// <summary>
    /// Rascunho editável.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Aguardando aprovação interna ou do cliente.
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Aprovado para agendamento.
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Agendado para publicação.
    /// </summary>
    Scheduled = 4,

    /// <summary>
    /// Publicação em andamento na rede.
    /// </summary>
    Publishing = 5,

    /// <summary>
    /// Publicado com sucesso.
    /// </summary>
    Published = 6,

    /// <summary>
    /// Rejeitado na aprovação.
    /// </summary>
    Rejected = 7,

    /// <summary>
    /// Cliente solicitou alterações.
    /// </summary>
    ChangesRequested = 8,

    /// <summary>
    /// Falha definitiva após tentativas de publicação.
    /// </summary>
    Failed = 9,

    /// <summary>
    /// Cancelado antes da publicação.
    /// </summary>
    Cancelled = 10,
}

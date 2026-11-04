namespace SocialPublishing.Application.Approvals;

/// <summary>
/// Decisão do cliente final no portal público de aprovação orgânica.
/// </summary>
public enum PublicApprovalDecision
{
    /// <summary>
    /// Aprova os posts selecionados.
    /// </summary>
    Approve = 1,

    /// <summary>
    /// Solicita alterações com comentário obrigatório.
    /// </summary>
    RequestChanges = 2,
}

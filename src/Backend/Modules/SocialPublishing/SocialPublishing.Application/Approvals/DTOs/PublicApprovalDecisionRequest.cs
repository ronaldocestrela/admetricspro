namespace SocialPublishing.Application.Approvals.DTOs;

/// <summary>
/// Payload de decisão pública de aprovação orgânica.
/// </summary>
/// <param name="PostIds">Identificadores dos posts selecionados.</param>
/// <param name="Decision">Aprovar ou solicitar alterações.</param>
/// <param name="Comment">Comentário obrigatório para solicitação de alterações.</param>
public sealed record PublicApprovalDecisionRequest(
    IReadOnlyList<Guid>? PostIds,
    PublicApprovalDecision Decision,
    string? Comment);

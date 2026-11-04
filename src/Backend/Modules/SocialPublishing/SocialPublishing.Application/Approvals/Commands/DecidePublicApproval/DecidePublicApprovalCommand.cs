using BuildingBlocks.Application.Messaging;
using SocialPublishing.Application.Approvals;

namespace SocialPublishing.Application.Approvals.Commands.DecidePublicApproval;

/// <summary>
/// Registra decisão do cliente final sobre posts selecionados via token público.
/// </summary>
/// <param name="PlainToken">Token opaco da URL.</param>
/// <param name="PostIds">Posts selecionados no quadro.</param>
/// <param name="Decision">Aprovar ou solicitar alterações.</param>
/// <param name="Comment">Comentário obrigatório quando <paramref name="Decision"/> é <see cref="PublicApprovalDecision.RequestChanges"/>.</param>
public sealed record DecidePublicApprovalCommand(
    string PlainToken,
    IReadOnlyList<Guid> PostIds,
    PublicApprovalDecision Decision,
    string? Comment) : ICommand;

using Automations.Application.Persistence;
using Automations.Domain.Rules;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Commands.DeleteRule;

/// <summary>
/// Comando para exclusão de uma regra de automação.
/// </summary>
public sealed record DeleteRuleCommand(
    Guid WorkspaceId,
    Guid RuleId) : ICommand;

/// <summary>
/// Manipulador do comando <see cref="DeleteRuleCommand"/>.
/// </summary>
public sealed class DeleteRuleCommandHandler : ICommandHandler<DeleteRuleCommand>
{
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IAutomationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="DeleteRuleCommandHandler"/>.
    /// </summary>
    public DeleteRuleCommandHandler(
        IAutomationRuleRepository ruleRepository,
        IAutomationsUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteRuleCommand command, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(command.RuleId, cancellationToken);
        if (rule is null)
        {
            return Result.Failure(Error.NotFound("AutomationRule.NotFound", "Regra não encontrada."));
        }

        if (rule.WorkspaceId != command.WorkspaceId)
        {
            return Result.Failure(Error.Validation("AutomationRule.WorkspaceMismatch", "A regra não pertence ao workspace informado."));
        }

        var deleteResult = await _ruleRepository.DeleteAsync(command.RuleId, cancellationToken);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

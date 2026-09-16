using Automations.Application.Persistence;
using Automations.Domain.Rules;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Commands.UpdateRule;

/// <summary>
/// Comando para atualização dos dados e lógica de uma regra existente.
/// </summary>
public sealed record UpdateRuleCommand(
    Guid WorkspaceId,
    Guid RuleId,
    string Name,
    string Description,
    RuleConditionGroup ConditionTree,
    List<RuleAction> Actions) : ICommand;

/// <summary>
/// Manipulador do comando <see cref="UpdateRuleCommand"/>.
/// </summary>
public sealed class UpdateRuleCommandHandler : ICommandHandler<UpdateRuleCommand>
{
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IAutomationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="UpdateRuleCommandHandler"/>.
    /// </summary>
    public UpdateRuleCommandHandler(
        IAutomationRuleRepository ruleRepository,
        IAutomationsUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(UpdateRuleCommand command, CancellationToken cancellationToken)
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

        var updateResult = rule.UpdateDetails(
            command.Name,
            command.Description,
            command.ConditionTree,
            command.Actions);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

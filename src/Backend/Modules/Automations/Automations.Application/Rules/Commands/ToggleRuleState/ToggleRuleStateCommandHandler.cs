using Automations.Application.Persistence;
using Automations.Domain.Rules;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Commands.ToggleRuleState;

/// <summary>
/// Comando para alternar o estado de ativação de uma regra.
/// </summary>
public sealed record ToggleRuleStateCommand(
    Guid WorkspaceId,
    Guid RuleId,
    bool IsEnabled) : ICommand;

/// <summary>
/// Manipulador do comando <see cref="ToggleRuleStateCommand"/>.
/// </summary>
public sealed class ToggleRuleStateCommandHandler : ICommandHandler<ToggleRuleStateCommand>
{
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IAutomationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ToggleRuleStateCommandHandler"/>.
    /// </summary>
    public ToggleRuleStateCommandHandler(
        IAutomationRuleRepository ruleRepository,
        IAutomationsUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result> Handle(ToggleRuleStateCommand command, CancellationToken cancellationToken)
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

        if (command.IsEnabled)
        {
            rule.Enable();
        }
        else
        {
            rule.Disable();
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}

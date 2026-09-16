using Automations.Application.Persistence;
using Automations.Domain.Rules;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Commands.CreateRule;

/// <summary>
/// Comando para criação de uma nova regra de automação cross-platform.
/// </summary>
public sealed record CreateRuleCommand(
    Guid WorkspaceId,
    string Name,
    string Description,
    RuleConditionGroup ConditionTree,
    List<RuleAction> Actions,
    bool IsEnabled = true) : ICommand<Guid>;

/// <summary>
/// Manipulador para criação de regra de automação com validação de invariantes e persistência.
/// </summary>
public sealed class CreateRuleCommandHandler : ICommandHandler<CreateRuleCommand, Guid>
{
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IAutomationsUnitOfWork _unitOfWork;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CreateRuleCommandHandler"/>.
    /// </summary>
    public CreateRuleCommandHandler(
        IAutomationRuleRepository ruleRepository,
        IAutomationsUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateRuleCommand command, CancellationToken cancellationToken)
    {
        var ruleId = Guid.NewGuid();
        var ruleResult = AutomationRule.Create(
            ruleId,
            command.WorkspaceId,
            command.Name,
            command.Description,
            command.ConditionTree,
            command.Actions,
            command.IsEnabled);

        if (ruleResult.IsFailure)
        {
            return Result<Guid>.Failure(ruleResult.Error);
        }

        var addResult = await _ruleRepository.AddAsync(ruleResult.Value, cancellationToken);
        if (addResult.IsFailure)
        {
            return Result<Guid>.Failure(addResult.Error);
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<Guid>.Success(ruleId);
    }
}

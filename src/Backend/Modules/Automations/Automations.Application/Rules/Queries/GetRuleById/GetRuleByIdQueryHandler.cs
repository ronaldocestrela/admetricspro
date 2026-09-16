using Automations.Application.Rules.DTOs;
using Automations.Domain.Rules;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Queries.GetRuleById;

/// <summary>
/// Consulta para obter detalhes de uma regra de automação pelo seu identificador.
/// </summary>
public sealed record GetRuleByIdQuery(
    Guid WorkspaceId,
    Guid RuleId) : IQuery<AutomationRuleDto>;

/// <summary>
/// Manipulador da consulta <see cref="GetRuleByIdQuery"/>.
/// </summary>
public sealed class GetRuleByIdQueryHandler : IQueryHandler<GetRuleByIdQuery, AutomationRuleDto>
{
    private readonly IAutomationRuleRepository _ruleRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="GetRuleByIdQueryHandler"/>.
    /// </summary>
    public GetRuleByIdQueryHandler(IAutomationRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
    }

    /// <inheritdoc />
    public async Task<Result<AutomationRuleDto>> Handle(GetRuleByIdQuery query, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByIdAsync(query.RuleId, cancellationToken);
        if (rule is null)
        {
            return Result<AutomationRuleDto>.Failure(
                Error.NotFound("AutomationRule.NotFound", "Regra de automação não localizada."));
        }

        if (rule.WorkspaceId != query.WorkspaceId)
        {
            return Result<AutomationRuleDto>.Failure(
                Error.Validation("AutomationRule.WorkspaceMismatch", "A regra não pertence ao workspace informado."));
        }

        var dto = new AutomationRuleDto(
            rule.Id,
            rule.WorkspaceId,
            rule.Name,
            rule.Description,
            rule.IsEnabled,
            rule.ConditionTree,
            rule.Actions,
            rule.LastTriggeredAtUtc,
            rule.ExecutionCount,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);

        return Result<AutomationRuleDto>.Success(dto);
    }
}

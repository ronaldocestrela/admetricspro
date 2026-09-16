using Automations.Application.Rules.DTOs;
using Automations.Domain.Rules;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace Automations.Application.Rules.Queries.ListRulesByWorkspace;

/// <summary>
/// Consulta para listar todas as regras de automação ativas e inativas do workspace.
/// </summary>
public sealed record ListRulesByWorkspaceQuery(
    Guid WorkspaceId) : IQuery<IReadOnlyList<AutomationRuleDto>>;

/// <summary>
/// Manipulador da consulta <see cref="ListRulesByWorkspaceQuery"/>.
/// </summary>
public sealed class ListRulesByWorkspaceQueryHandler : IQueryHandler<ListRulesByWorkspaceQuery, IReadOnlyList<AutomationRuleDto>>
{
    private readonly IAutomationRuleRepository _ruleRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ListRulesByWorkspaceQueryHandler"/>.
    /// </summary>
    public ListRulesByWorkspaceQueryHandler(IAutomationRuleRepository ruleRepository)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AutomationRuleDto>>> Handle(ListRulesByWorkspaceQuery query, CancellationToken cancellationToken)
    {
        var rules = await _ruleRepository.GetByWorkspaceIdAsync(query.WorkspaceId, cancellationToken);

        var dtos = rules.Select(rule => new AutomationRuleDto(
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
            rule.UpdatedAtUtc)).ToList();

        return Result<IReadOnlyList<AutomationRuleDto>>.Success(dtos);
    }
}

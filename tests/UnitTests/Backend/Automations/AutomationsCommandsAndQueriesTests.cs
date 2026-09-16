using Automations.Application.Persistence;
using Automations.Application.Rules.Commands.CreateRule;
using Automations.Application.Rules.Commands.DeleteRule;
using Automations.Application.Rules.Commands.EvaluateRule;
using Automations.Application.Rules.Commands.ToggleRuleState;
using Automations.Application.Rules.Commands.UpdateRule;
using Automations.Application.Rules.DTOs;
using Automations.Application.Rules.Queries.GetRuleById;
using Automations.Application.Rules.Queries.ListRulesByWorkspace;
using Automations.Application.Rules.Services;
using Automations.Domain.Rules;
using Automations.Domain.Services;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para os handlers CQRS do módulo Automations (comandos e consultas de regras).
/// </summary>
public sealed class AutomationsCommandsAndQueriesTests
{
    private readonly IAutomationRuleRepository _ruleRepository = Substitute.For<IAutomationRuleRepository>();
    private readonly IAutomationsUnitOfWork _unitOfWork = Substitute.For<IAutomationsUnitOfWork>();
    private readonly IAutomationsMetricsProvider _metricsProvider = Substitute.For<IAutomationsMetricsProvider>();
    private readonly IRuleConditionEvaluator _conditionEvaluator = Substitute.For<IRuleConditionEvaluator>();
    private readonly IRuleActionDispatcher _actionDispatcher = Substitute.For<IRuleActionDispatcher>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida que CreateRuleCommandHandler persiste a nova regra com sucesso e retorna seu ID.
    /// </summary>
    [Fact]
    public async Task CreateRule_ShouldSucceed_WhenPayloadIsValid()
    {
        var handler = new CreateRuleCommandHandler(_ruleRepository, _unitOfWork);
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var command = new CreateRuleCommand(_workspaceId, "Pausa TikTok", "Descrição", group, new List<RuleAction> { action });

        _ruleRepository.AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>())
            .Returns(ci => Result<AutomationRule>.Success(ci.Arg<AutomationRule>()));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _ruleRepository.Received(1).AddAsync(Arg.Any<AutomationRule>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que CreateRuleCommandHandler falha quando o nome da regra está em branco.
    /// </summary>
    [Fact]
    public async Task CreateRule_ShouldFail_WhenNameIsEmpty()
    {
        var handler = new CreateRuleCommandHandler(_ruleRepository, _unitOfWork);
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var command = new CreateRuleCommand(_workspaceId, " ", "Descrição", group, new List<RuleAction> { action });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AutomationRule.EmptyName");
    }

    /// <summary>
    /// Valida que UpdateRuleCommandHandler atualiza os dados da regra com sucesso.
    /// </summary>
    [Fact]
    public async Task UpdateRule_ShouldSucceed_WhenRuleExists()
    {
        var ruleId = Guid.NewGuid();
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var rule = AutomationRule.Create(ruleId, _workspaceId, "Original", "Desc", group, new[] { action }).Value;
        _ruleRepository.GetByIdAsync(ruleId, Arg.Any<CancellationToken>()).Returns(rule);

        var handler = new UpdateRuleCommandHandler(_ruleRepository, _unitOfWork);
        var command = new UpdateRuleCommand(_workspaceId, ruleId, "Novo Nome", "Nova Desc", group, new List<RuleAction> { action });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        rule.Name.Should().Be("Novo Nome");
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ToggleRuleStateCommandHandler ativa ou desativa uma regra existente.
    /// </summary>
    [Fact]
    public async Task ToggleRuleState_ShouldUpdateIsEnabled()
    {
        var ruleId = Guid.NewGuid();
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var rule = AutomationRule.Create(ruleId, _workspaceId, "Regra", "Desc", group, new[] { action }, isEnabled: true).Value;
        _ruleRepository.GetByIdAsync(ruleId, Arg.Any<CancellationToken>()).Returns(rule);

        var handler = new ToggleRuleStateCommandHandler(_ruleRepository, _unitOfWork);
        var command = new ToggleRuleStateCommand(_workspaceId, ruleId, IsEnabled: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        rule.IsEnabled.Should().BeFalse();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que DeleteRuleCommandHandler remove a regra do repositório.
    /// </summary>
    [Fact]
    public async Task DeleteRule_ShouldSucceed_WhenRuleExists()
    {
        var ruleId = Guid.NewGuid();
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var rule = AutomationRule.Create(ruleId, _workspaceId, "Regra", "Desc", group, new[] { action }).Value;
        _ruleRepository.GetByIdAsync(ruleId, Arg.Any<CancellationToken>()).Returns(rule);
        _ruleRepository.DeleteAsync(ruleId, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new DeleteRuleCommandHandler(_ruleRepository, _unitOfWork);
        var command = new DeleteRuleCommand(_workspaceId, ruleId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _ruleRepository.Received(1).DeleteAsync(ruleId, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que GetRuleByIdQueryHandler retorna o DTO correto para uma regra existente.
    /// </summary>
    [Fact]
    public async Task GetRuleById_ShouldReturnDto_WhenRuleExists()
    {
        var ruleId = Guid.NewGuid();
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var rule = AutomationRule.Create(ruleId, _workspaceId, "Regra Teste", "Desc Teste", group, new[] { action }).Value;
        _ruleRepository.GetByIdAsync(ruleId, Arg.Any<CancellationToken>()).Returns(rule);

        var handler = new GetRuleByIdQueryHandler(_ruleRepository);
        var query = new GetRuleByIdQuery(_workspaceId, ruleId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Regra Teste");
    }

    /// <summary>
    /// Valida que ListRulesByWorkspaceQueryHandler retorna a coleção de regras cadastradas.
    /// </summary>
    [Fact]
    public async Task ListRulesByWorkspace_ShouldReturnAllRules()
    {
        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var rule1 = AutomationRule.Create(Guid.NewGuid(), _workspaceId, "Regra 1", "Desc", group, new[] { action }).Value;
        var rule2 = AutomationRule.Create(Guid.NewGuid(), _workspaceId, "Regra 2", "Desc", group, new[] { action }).Value;

        _ruleRepository.GetByWorkspaceIdAsync(_workspaceId, Arg.Any<CancellationToken>())
            .Returns(new[] { rule1, rule2 });

        var handler = new ListRulesByWorkspaceQueryHandler(_ruleRepository);
        var query = new ListRulesByWorkspaceQuery(_workspaceId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    /// <summary>
    /// Valida que EvaluateRuleCommandHandler avalia a regra, despacha mutações quando disparada e atualiza estatísticas.
    /// </summary>
    [Fact]
    public async Task EvaluateRule_ShouldTriggerAndDispatchActions_WhenConditionsMet()
    {
        var ruleId = Guid.NewGuid();
        var targetCampaignId = Guid.NewGuid();

        var condition = new MetricPredicate("TikTokAds", RuleScope.Campaign, MetricType.Cpa, ComparisonOperator.GreaterThan, 50m, 48, targetCampaignId);
        var group = new RuleConditionGroup(LogicalOperator.And, new[] { condition });
        var action = new RuleAction(RuleActionType.PauseCampaign, targetEntityId: targetCampaignId);

        var rule = AutomationRule.Create(ruleId, _workspaceId, "Alarme CPA", "Desc", group, new[] { action }).Value;
        _ruleRepository.GetByIdAsync(ruleId, Arg.Any<CancellationToken>()).Returns(rule);

        var context = new RuleEvaluationContext(_workspaceId);
        _metricsProvider.LoadMetricsContextAsync(_workspaceId, Arg.Any<CancellationToken>()).Returns(context);

        var outcome = new EvaluationOutcome(true, new[] { targetCampaignId }, 1, new[] { "Disparou" });
        _conditionEvaluator.Evaluate(group, context).Returns(outcome);

        _actionDispatcher.DispatchActionsAsync(_workspaceId, Arg.Any<IEnumerable<RuleAction>>(), Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(1));

        var handler = new EvaluateRuleCommandHandler(
            _ruleRepository,
            _metricsProvider,
            _conditionEvaluator,
            _actionDispatcher,
            _unitOfWork);

        var command = new EvaluateRuleCommand(_workspaceId, ruleId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsTriggered.Should().BeTrue();
        result.Value.ActionsDispatchedCount.Should().Be(1);
        rule.ExecutionCount.Should().Be(1);
        rule.LastTriggeredAtUtc.Should().NotBeNull();
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

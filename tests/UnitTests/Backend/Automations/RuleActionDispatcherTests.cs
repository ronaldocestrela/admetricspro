using Automations.Application.Rules.Services;
using BuildingBlocks.Domain.Automations;
using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Automations;

/// <summary>
/// Testes unitários para o despachador de ações RuleActionDispatcher (Subfase 4.1.3 - TDD).
/// Valida a tradução de ações da DSL em comandos do MediatR para o módulo Integrations.
/// </summary>
public sealed class RuleActionDispatcherTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly RuleActionDispatcher _dispatcher;
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa a suíte de testes configurando o dispatcher com mock do MediatR.
    /// </summary>
    public RuleActionDispatcherTests()
    {
        _dispatcher = new RuleActionDispatcher(_sender);
        _sender.Send(Arg.Any<IRequest<Result>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
    }

    /// <summary>
    /// Valida o envio de PauseCampaignCommand para campanha alvo específica.
    /// </summary>
    [Fact]
    public async Task DispatchActionsAsync_ShouldSendPauseCampaignCommand_WhenExplicitTargetSpecified()
    {
        var campaignId = Guid.NewGuid();
        var action = new RuleAction(RuleActionType.PauseCampaign, targetEntityId: campaignId);

        var result = await _dispatcher.DispatchActionsAsync(_workspaceId, new[] { action }, Array.Empty<Guid>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        await _sender.Received(1).Send(
            Arg.Is<PauseCampaignCommand>(cmd => cmd.WorkspaceId == _workspaceId && cmd.CampaignId == campaignId),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida o envio de PauseCampaignCommand para todas as campanhas correspondidas quando nenhum alvo explícito for definido.
    /// </summary>
    [Fact]
    public async Task DispatchActionsAsync_ShouldSendPauseCampaignCommand_ForMatchedEntities_WhenNoTargetSpecified()
    {
        var matchedId1 = Guid.NewGuid();
        var matchedId2 = Guid.NewGuid();
        var action = new RuleAction(RuleActionType.PauseCampaign);

        var result = await _dispatcher.DispatchActionsAsync(_workspaceId, new[] { action }, new[] { matchedId1, matchedId2 });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2);
        await _sender.Received(1).Send(
            Arg.Is<PauseCampaignCommand>(cmd => cmd.WorkspaceId == _workspaceId && cmd.CampaignId == matchedId1),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<PauseCampaignCommand>(cmd => cmd.WorkspaceId == _workspaceId && cmd.CampaignId == matchedId2),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida o envio de AdjustCampaignBudgetCommand com percentual de variação orçamentária.
    /// </summary>
    [Fact]
    public async Task DispatchActionsAsync_ShouldSendAdjustBudgetCommand_WithPercentage()
    {
        var campaignId = Guid.NewGuid();
        var action = new RuleAction(RuleActionType.AdjustBudgetPercentage, targetEntityId: campaignId, value: -20m);

        var result = await _dispatcher.DispatchActionsAsync(_workspaceId, new[] { action }, Array.Empty<Guid>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        await _sender.Received(1).Send(
            Arg.Is<AdjustCampaignBudgetCommand>(cmd =>
                cmd.WorkspaceId == _workspaceId &&
                cmd.CampaignId == campaignId &&
                cmd.PercentageChange == -20m),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida o envio de ReallocateBudgetCommand com transferência de saldo entre plataformas/campanhas.
    /// </summary>
    [Fact]
    public async Task DispatchActionsAsync_ShouldSendReallocateBudgetCommand()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var action = new RuleAction(
            RuleActionType.ReallocateBudget,
            targetEntityId: sourceId,
            destinationEntityId: targetId,
            value: 20m);

        var result = await _dispatcher.DispatchActionsAsync(_workspaceId, new[] { action }, Array.Empty<Guid>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        await _sender.Received(1).Send(
            Arg.Is<ReallocateBudgetCommand>(cmd =>
                cmd.WorkspaceId == _workspaceId &&
                cmd.SourceCampaignId == sourceId &&
                cmd.TargetCampaignId == targetId &&
                cmd.AmountOrPercentage == 20m &&
                cmd.IsPercentage),
            Arg.Any<CancellationToken>());
    }
}

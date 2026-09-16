using Analytics.Application.Copilot.DTOs;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using UnitTests.Frontend.Common;
using WebApp.Components.Copilot;
using Xunit;

namespace UnitTests.Frontend.Components.Copilot;

/// <summary>
/// Testes bUnit para o componente <see cref="CopilotAnomalyCard"/>.
/// </summary>
public sealed class CopilotAnomalyCardTests : BunitTestBase
{
    /// <summary>
    /// Valida que o card renderiza título, severidade, detalhes da ação e aciona o callback de execução em 1 clique.
    /// </summary>
    [Fact]
    public async Task CopilotAnomalyCard_ShouldTriggerOnExecuteAction_WhenButtonClicked()
    {
        // Arrange
        var actionDto = new CopilotRecommendationActionDto(
            ActionId: Guid.NewGuid(),
            ActionType: "PauseAdSet",
            TargetEntityId: Guid.NewGuid(),
            TargetEntityName: "Conjunto Redundante B",
            Platform: "MetaAds",
            Title: "Pausar conjunto redundante",
            Description: "Pausar conjunto de pior CPA",
            Parameters: new Dictionary<string, string>(),
            IsApplied: false,
            AppliedAtUtc: null
        );

        CopilotRecommendationActionDto? executedAction = null;
        var onExecute = EventCallback.Factory.Create<CopilotRecommendationActionDto>(this, (action) =>
        {
            executedAction = action;
        });

        // Act
        var cut = Render<CopilotAnomalyCard>(parameters => parameters
            .Add(p => p.Title, "Sobreposição de Públicos (Meta Ads)")
            .Add(p => p.Severity, "Critical")
            .Add(p => p.Description, "Sobreposição prejudicial detectada")
            .Add(p => p.Action, actionDto)
            .Add(p => p.OnExecuteAction, onExecute));

        // Assert inicial
        cut.Markup.Should().Contain("Sobreposição de Públicos (Meta Ads)");
        cut.Markup.Should().Contain("Critical");
        cut.Markup.Should().Contain("Sobreposição prejudicial detectada");

        var button = cut.Find($"#btn-execute-{actionDto.ActionId}");
        button.Should().NotBeNull();
        button.TextContent.Should().Contain("Executar em 1 Clique");

        // Simula o clique do usuário no botão de 1 clique
        await cut.InvokeAsync(() => button.Click());

        // Assert pós-clique
        executedAction.Should().NotBeNull();
        executedAction!.ActionId.Should().Be(actionDto.ActionId);
        cut.Markup.Should().Contain("Aplicado");
    }
}

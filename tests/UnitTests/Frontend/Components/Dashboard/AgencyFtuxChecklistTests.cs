using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Ftux.DTOs;
using UnitTests.Frontend.Common;
using WebApp.Components.Dashboard;
using Xunit;

namespace UnitTests.Frontend.Components.Dashboard;

/// <summary>
/// Testes unitários com bUnit para o componente <see cref="AgencyFtuxChecklist"/>.
/// </summary>
public sealed class AgencyFtuxChecklistTests : BunitTestBase
{
    /// <summary>
    /// Valida que o checklist renderiza inicialmente o progresso de 25% com o Passo 1 concluído e botões de ação nos passos pendentes.
    /// </summary>
    [Fact]
    public void AgencyFtuxChecklist_ShouldRender25PercentInitialProgress()
    {
        // Act
        var cut = Render<AgencyFtuxChecklist>();

        // Assert
        var progressPercent = cut.Find("#ftux-progress-percent");
        progressPercent.TextContent.Trim().Should().Be("25%");

        var step1Badge = cut.Find(".ftux-step-row.completed .badge-done");
        step1Badge.Should().NotBeNull();
        step1Badge.TextContent.Trim().Should().Be("Concluído");

        cut.Find("#btn-step-workspace").Should().NotBeNull();
        cut.Find("#btn-step-connection").Should().NotBeNull();
        cut.Find("#btn-step-team").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que quando todos os 4 passos estão completos, o progresso atinge 100% e exibe o banner de celebração.
    /// </summary>
    [Fact]
    public void AgencyFtuxChecklist_WhenAllStepsDone_ShouldRender100PercentAndCelebrationBanner()
    {
        // Arrange
        TenantFtuxClientService.GetFtuxStatusAsync(Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result<TenantFtuxStatusDto>.Success(
                new TenantFtuxStatusDto(
                    IsProvisioned: true,
                    WorkspacesCount: 2,
                    ConnectedAdAccountsCount: 2,
                    TeamMembersCount: 3,
                    SquadsCount: 1,
                    Step1Completed: true,
                    Step2Completed: true,
                    Step3Completed: true,
                    Step4Completed: true,
                    ProgressPercentage: 100,
                    IsCompleted: true)));

        // Act
        var cut = Render<AgencyFtuxChecklist>();

        // Assert
        var progressPercent = cut.Find("#ftux-progress-percent");
        progressPercent.TextContent.Trim().Should().Be("100%");

        var banner = cut.Find(".ftux-completion-banner");
        banner.Should().NotBeNull();
        banner.TextContent.Should().Contain("Tudo pronto! Sua agência está 100% configurada");

        // Todos os passos devem exibir a badge Concluído
        var completedBadges = cut.FindAll(".badge-done");
        completedBadges.Should().HaveCount(4);
    }

    /// <summary>
    /// Valida que ao clicar no botão 'Cadastrar Cliente', o modal de workspace é aberto.
    /// </summary>
    [Fact]
    public void AgencyFtuxChecklist_WhenClickingWorkspaceStep_ShouldOpenWorkspaceModal()
    {
        // Arrange
        var cut = Render<AgencyFtuxChecklist>();

        // Act
        var btn = cut.Find("#btn-step-workspace");
        btn.Click();

        // Assert
        cut.Find("#modal-workspace-title").Should().NotBeNull();
        cut.Find("#workspace-name-input").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao clicar no botão 'Conectar Anúncios', o modal de conexões de anúncios é aberto.
    /// </summary>
    [Fact]
    public void AgencyFtuxChecklist_WhenClickingConnectionStep_ShouldOpenConnectionModal()
    {
        // Arrange
        var cut = Render<AgencyFtuxChecklist>();

        // Act
        var btn = cut.Find("#btn-step-connection");
        btn.Click();

        // Assert
        cut.Find("#modal-ad-title").Should().NotBeNull();
        cut.Find("#btn-load-demo-ads").Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ao clicar no botão 'Configurar Equipe', o modal de equipe é aberto.
    /// </summary>
    [Fact]
    public void AgencyFtuxChecklist_WhenClickingTeamStep_ShouldOpenTeamModal()
    {
        // Arrange
        var cut = Render<AgencyFtuxChecklist>();

        // Act
        var btn = cut.Find("#btn-step-team");
        btn.Click();

        // Assert
        cut.Find("#modal-team-title").Should().NotBeNull();
        cut.Find("#member-name-input").Should().NotBeNull();
    }
}

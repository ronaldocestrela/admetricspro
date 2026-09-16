using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using Bunit;
using FluentAssertions;
using Integrations.Application.Campaigns.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using WebApp.Components.Campaigns;
using WebApp.Services;
using Xunit;

namespace UnitTests.Frontend.Components.Campaigns;

/// <summary>
/// Testes com bUnit para a tabela matricial de edição e operações em massa <see cref="BulkCampaignEditorMatrix"/> (Subfase 5.1.3).
/// Valida renderização, seleção em massa, pré-visualização de impacto orçamentário e feedback pós-execução.
/// </summary>
public sealed class BulkCampaignEditorMatrixTests : BunitTestBase
{
    private readonly IBulkCampaignClientService _bulkClientService = Substitute.For<IBulkCampaignClientService>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Inicializa o contexto de teste registrando o mock do serviço de operações em lote.
    /// </summary>
    public BulkCampaignEditorMatrixTests()
    {
        Services.AddSingleton(_bulkClientService);
    }

    private List<CampaignHierarchyDto> CreateSampleCampaigns()
    {
        return new List<CampaignHierarchyDto>
        {
            new(
                Guid.NewGuid(),
                _workspaceId,
                Guid.NewGuid(),
                "MetaAds",
                "ext-meta-1",
                "Meta Conversões Brasil",
                "Active",
                "Conversions",
                100m,
                null,
                "BRL",
                DateTime.UtcNow,
                null,
                DateTime.UtcNow,
                Array.Empty<AdSetHierarchyDto>()),
            new(
                Guid.NewGuid(),
                _workspaceId,
                Guid.NewGuid(),
                "GoogleAds",
                "ext-goog-1",
                "Google Pesquisa Top Brand",
                "Active",
                "Search",
                200m,
                null,
                "BRL",
                DateTime.UtcNow,
                null,
                DateTime.UtcNow,
                Array.Empty<AdSetHierarchyDto>()),
            new(
                Guid.NewGuid(),
                _workspaceId,
                Guid.NewGuid(),
                "TikTokAds",
                "ext-tik-1",
                "TikTok Viral Criativos",
                "Paused",
                "VideoViews",
                50m,
                null,
                "BRL",
                DateTime.UtcNow,
                null,
                DateTime.UtcNow,
                Array.Empty<AdSetHierarchyDto>())
        };
    }

    /// <summary>
    /// Valida que a tabela matricial renderiza todas as campanhas, nomes, plataformas e badges.
    /// </summary>
    [Fact]
    public void Render_ShouldDisplayAllCampaignsAndPlatformBadges()
    {
        // Arrange
        var campaigns = CreateSampleCampaigns();

        // Act
        var cut = Render<BulkCampaignEditorMatrix>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId)
            .Add(p => p.Campaigns, campaigns));

        // Assert
        cut.Find("[data-testid='bulk-campaign-matrix']").Should().NotBeNull();
        cut.Find("[data-testid='matrix-table']").Should().NotBeNull();

        var rows = cut.FindAll("tbody tr");
        rows.Should().HaveCount(3);

        cut.Markup.Should().Contain("Meta Conversões Brasil");
        cut.Markup.Should().Contain("Google Pesquisa Top Brand");
        cut.Markup.Should().Contain("TikTok Viral Criativos");
        cut.Markup.Should().Contain("platform-metaads");
        cut.Markup.Should().Contain("platform-googleads");
        cut.Markup.Should().Contain("platform-tiktokads");
    }

    /// <summary>
    /// Valida que o checkbox máster no cabeçalho seleciona todas as campanhas e exibe a barra de ações em lote.
    /// </summary>
    [Fact]
    public void SelectAll_ShouldToggleSelectionForAllFilteredCampaigns()
    {
        // Arrange
        var campaigns = CreateSampleCampaigns();
        var cut = Render<BulkCampaignEditorMatrix>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId)
            .Add(p => p.Campaigns, campaigns));

        // Act: Marca o checkbox de selecionar tudo
        var selectAllCheckbox = cut.Find("[data-testid='select-all-checkbox']");
        selectAllCheckbox.Change(true);

        // Assert
        cut.Find("[data-testid='bulk-action-bar']").Should().NotBeNull();
        cut.Find(".selection-badge").TextContent.Trim().Should().Be("3");
        cut.FindAll("tr.row-selected").Should().HaveCount(3);
    }

    /// <summary>
    /// Valida que ao acionar reajuste de orçamento, o modal de pré-visualização de impacto é aberto e calcula as projeções corretamente.
    /// </summary>
    [Fact]
    public void OpenPreviewModal_ShouldCalculateBudgetImpactCorrectly()
    {
        // Arrange
        var campaigns = CreateSampleCampaigns(); // Totais: Meta(100) + Google(200) + TikTok(50) = 350
        var cut = Render<BulkCampaignEditorMatrix>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId)
            .Add(p => p.Campaigns, campaigns));

        // Seleciona todas
        cut.Find("[data-testid='select-all-checkbox']").Change(true);

        // Define reajuste de +20%
        var inputVal = cut.Find("[data-testid='input-budget-value']");
        inputVal.Change(20m);

        // Act: Clica em Aplicar Orçamento
        var btnAdjust = cut.Find("[data-testid='btn-bulk-adjust']");
        btnAdjust.Click();

        // Assert: Modal de prévia deve estar visível
        var modal = cut.Find("[data-testid='preview-modal']");
        modal.Should().NotBeNull();

        // Total diário atual = 350. Novo projetado = 350 * 1.20 = 420. Variação = +70
        modal.TextContent.Should().Contain("350,00");
        modal.TextContent.Should().Contain("420,00");
        modal.TextContent.Should().Contain("70,00");
        modal.TextContent.Should().Contain("20");
    }

    /// <summary>
    /// Valida que a confirmação no modal despacha o comando para o serviço e renderiza o relatório pós-execução.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ConfirmBulkOperation_ShouldInvokeServiceAndDisplayExecutionReport()
    {
        // Arrange
        var campaigns = CreateSampleCampaigns();
        var expectedResult = new BulkCampaignOperationResultDto
        {
            TotalRequested = 2,
            TotalSucceeded = 2,
            TotalFailed = 0,
            SucceededItems = new List<BulkOperationSuccessItemDto>
            {
                new(campaigns[0].Id, campaigns[0].Name, "MetaAds", BulkCampaignActionType.Pause, CampaignStatus.Active, CampaignStatus.Paused, 100m, 100m),
                new(campaigns[1].Id, campaigns[1].Name, "GoogleAds", BulkCampaignActionType.Pause, CampaignStatus.Active, CampaignStatus.Paused, 200m, 200m)
            }
        };

        _bulkClientService.ExecuteBulkOperationsAsync(_workspaceId, Arg.Any<IReadOnlyList<BulkCampaignOperationItem>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BulkCampaignOperationResultDto>.Success(expectedResult)));

        var cut = Render<BulkCampaignEditorMatrix>(parameters => parameters
            .Add(p => p.WorkspaceId, _workspaceId)
            .Add(p => p.Campaigns, campaigns));

        // Seleciona as duas primeiras campanhas
        cut.Find($"[data-testid='checkbox-{campaigns[0].Id}']").Change(true);
        cut.Find($"[data-testid='checkbox-{campaigns[1].Id}']").Change(true);

        // Abre modal para Pausar
        cut.Find("[data-testid='btn-bulk-pause']").Click();

        // Act: Confirma a operação no modal
        var btnConfirm = cut.Find("[data-testid='btn-confirm-bulk']");
        await cut.InvokeAsync(() => btnConfirm.Click());

        // Assert: Serviço invocado com 2 itens
        await _bulkClientService.Received(1).ExecuteBulkOperationsAsync(
            _workspaceId,
            Arg.Is<IReadOnlyList<BulkCampaignOperationItem>>(list => list.Count == 2),
            Arg.Any<CancellationToken>());

        // Relatório de execução renderizado
        cut.Find("[data-testid='execution-report']").Should().NotBeNull();
        cut.Find(".pill-success").TextContent.Should().Contain("2");
    }
}

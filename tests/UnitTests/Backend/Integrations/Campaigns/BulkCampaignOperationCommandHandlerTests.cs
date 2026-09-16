using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.Mutations;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes unitários para o manipulador de operações em massa multiplataforma (Subfase 5.1.1 e 5.1.2).
/// Valida execução em lote sobre 30 campanhas de diferentes redes de anúncios e tolerância a falhas parciais.
/// </summary>
public sealed class BulkCampaignOperationCommandHandlerTests
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository = Substitute.For<ICampaignHierarchyRepository>();
    private readonly IIntegrationsUnitOfWork _unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida que o processamento em lote de 30 campanhas multiplataforma (Meta, Google, TikTok, Bing) tem êxito completo com commit.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task Handle_ShouldSucceed_WhenProcessing30CrossPlatformCampaignsInBatch()
    {
        // Arrange: 30 campanhas distribuídas entre 4 plataformas (Meta, Google, TikTok, Bing)
        var platforms = new[] { "MetaAds", "GoogleAds", "TikTokAds", "BingAds" };
        var campaigns = new List<Campaign>();
        var operations = new List<BulkCampaignOperationItem>();

        for (int i = 0; i < 30; i++)
        {
            var campaignId = Guid.NewGuid();
            var platform = platforms[i % platforms.Length];
            var initialStatus = i % 2 == 0 ? CampaignStatus.Paused : CampaignStatus.Active;
            var initialBudget = 100m + (i * 10m);

            var campaign = Campaign.Create(
                campaignId,
                _workspaceId,
                Guid.NewGuid(),
                platform,
                $"ext-cmp-{i}",
                $"Campanha {platform} #{i}",
                initialStatus,
                "Conversions",
                dailyBudget: initialBudget).Value;

            campaigns.Add(campaign);

            _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
                .Returns(campaign);

            // Alterna tipos de ação: 0-9 Ativar, 10-19 Pausar, 20-29 Reajuste Orçamentário
            if (i < 10)
            {
                operations.Add(new BulkCampaignOperationItem(campaignId, BulkCampaignActionType.Activate));
            }
            else if (i < 20)
            {
                operations.Add(new BulkCampaignOperationItem(campaignId, BulkCampaignActionType.Pause));
            }
            else
            {
                // Reajuste percentual (+15%) ou fixo (250m)
                if (i % 2 == 0)
                {
                    operations.Add(new BulkCampaignOperationItem(
                        campaignId,
                        BulkCampaignActionType.AdjustBudget,
                        PercentageChange: 15m));
                }
                else
                {
                    operations.Add(new BulkCampaignOperationItem(
                        campaignId,
                        BulkCampaignActionType.AdjustBudget,
                        DailyBudget: 250m));
                }
            }
        }

        var handler = new BulkCampaignOperationCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new BulkCampaignOperationCommand(_workspaceId, operations);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TotalRequested.Should().Be(30);
        result.Value.TotalSucceeded.Should().Be(30);
        result.Value.TotalFailed.Should().Be(0);
        result.Value.SucceededItems.Should().HaveCount(30);
        result.Value.FailedItems.Should().BeEmpty();

        // Verifica que as campanhas sofreram as mutações corretas
        for (int i = 0; i < 10; i++)
        {
            campaigns[i].Status.Should().Be(CampaignStatus.Active);
        }
        for (int i = 10; i < 20; i++)
        {
            campaigns[i].Status.Should().Be(CampaignStatus.Paused);
        }
        for (int i = 20; i < 30; i++)
        {
            if (i % 2 == 0)
            {
                var expected = Math.Round((100m + (i * 10m)) * 1.15m, 2);
                campaigns[i].DailyBudget.Should().Be(expected);
            }
            else
            {
                campaigns[i].DailyBudget.Should().Be(250m);
            }
        }

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida o padrão de tolerância a falhas parciais onde itens inválidos são reportados e os válidos são aplicados e comitados.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task Handle_ShouldToleratePartialFailures_WhenSomeCampaignsFailValidation()
    {
        // Arrange
        // Campanha 1: Válida (Pausar)
        var camp1Id = Guid.NewGuid();
        var camp1 = Campaign.Create(
            camp1Id,
            _workspaceId,
            Guid.NewGuid(),
            "MetaAds",
            "ext-1",
            "Campanha 1",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 150m).Value;

        // Campanha 2: Inexistente no banco
        var camp2Id = Guid.NewGuid();

        // Campanha 3: Pertence a outro workspace (WorkspaceMismatch)
        var camp3Id = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var camp3 = Campaign.Create(
            camp3Id,
            otherWorkspaceId,
            Guid.NewGuid(),
            "GoogleAds",
            "ext-3",
            "Campanha 3 Outro Workspace",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 100m).Value;

        // Campanha 4: Reajuste percentual com budget zerado
        var camp4Id = Guid.NewGuid();
        var camp4 = Campaign.Create(
            camp4Id,
            _workspaceId,
            Guid.NewGuid(),
            "TikTokAds",
            "ext-4",
            "Campanha 4 Sem Budget",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 0m).Value;

        // Campanha 5: Válida (Ativar)
        var camp5Id = Guid.NewGuid();
        var camp5 = Campaign.Create(
            camp5Id,
            _workspaceId,
            Guid.NewGuid(),
            "BingAds",
            "ext-5",
            "Campanha 5",
            CampaignStatus.Paused,
            "Conversions",
            dailyBudget: 80m).Value;

        _hierarchyRepository.GetCampaignByIdAsync(camp1Id, Arg.Any<CancellationToken>()).Returns(camp1);
        _hierarchyRepository.GetCampaignByIdAsync(camp2Id, Arg.Any<CancellationToken>()).Returns((Campaign?)null);
        _hierarchyRepository.GetCampaignByIdAsync(camp3Id, Arg.Any<CancellationToken>()).Returns(camp3);
        _hierarchyRepository.GetCampaignByIdAsync(camp4Id, Arg.Any<CancellationToken>()).Returns(camp4);
        _hierarchyRepository.GetCampaignByIdAsync(camp5Id, Arg.Any<CancellationToken>()).Returns(camp5);

        var operations = new List<BulkCampaignOperationItem>
        {
            new(camp1Id, BulkCampaignActionType.Pause),
            new(camp2Id, BulkCampaignActionType.Activate),
            new(camp3Id, BulkCampaignActionType.Pause),
            new(camp4Id, BulkCampaignActionType.AdjustBudget, PercentageChange: 20m),
            new(camp5Id, BulkCampaignActionType.Activate)
        };

        var handler = new BulkCampaignOperationCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new BulkCampaignOperationCommand(_workspaceId, operations);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRequested.Should().Be(5);
        result.Value.TotalSucceeded.Should().Be(2); // camp1 e camp5
        result.Value.TotalFailed.Should().Be(3);    // camp2, camp3, camp4

        result.Value.SucceededItems.Should().HaveCount(2);
        result.Value.FailedItems.Should().HaveCount(3);

        // camp1 pausada
        camp1.Status.Should().Be(CampaignStatus.Paused);
        // camp5 ativada
        camp5.Status.Should().Be(CampaignStatus.Active);

        // Verifica erros específicos
        result.Value.FailedItems.Should().Contain(f => f.CampaignId == camp2Id && f.ErrorCode == "Campaign.NotFound");
        result.Value.FailedItems.Should().Contain(f => f.CampaignId == camp3Id && f.ErrorCode == "Campaign.WorkspaceMismatch");
        result.Value.FailedItems.Should().Contain(f => f.CampaignId == camp4Id && f.ErrorCode == "Campaign.CurrentBudgetZero");

        // Commit foi realizado para as alterações válidas
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a tentativa de processar lote com lista de operações vazia retorna erro de validação.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenOperationsListIsEmpty()
    {
        var handler = new BulkCampaignOperationCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new BulkCampaignOperationCommand(_workspaceId, Array.Empty<BulkCampaignOperationItem>());

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BulkCampaign.EmptyList");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a tentativa de processar lote com identificador de workspace vazio retorna erro de validação.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenWorkspaceIdIsEmpty()
    {
        var handler = new BulkCampaignOperationCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new BulkCampaignOperationCommand(Guid.Empty, new[]
        {
            new BulkCampaignOperationItem(Guid.NewGuid(), BulkCampaignActionType.Pause)
        });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BulkCampaign.InvalidWorkspaceId");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que lotes com tamanho superior ao limite máximo permitido de 100 operações retornam erro de validação.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenBatchSizeExceedsMaximumLimit()
    {
        var handler = new BulkCampaignOperationCommandHandler(_hierarchyRepository, _unitOfWork);
        var operations = Enumerable.Range(1, 101)
            .Select(_ => new BulkCampaignOperationItem(Guid.NewGuid(), BulkCampaignActionType.Pause))
            .ToList();

        var command = new BulkCampaignOperationCommand(_workspaceId, operations);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BulkCampaign.BatchLimitExceeded");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}

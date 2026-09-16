using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Campaigns;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.Mutations;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Testes unitários para os handlers de mutação do módulo Integrations acionados por regras de automação (Subfase 4.1.3).
/// </summary>
public sealed class IntegrationsMutationCommandHandlersTests
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository = Substitute.For<ICampaignHierarchyRepository>();
    private readonly IIntegrationsUnitOfWork _unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida que PauseCampaignCommandHandler altera o status da campanha para Paused e executa commit.
    /// </summary>
    [Fact]
    public async Task PauseCampaign_ShouldSucceed_WhenCampaignExistsAndBelongsToWorkspace()
    {
        var campaignId = Guid.NewGuid();
        var campaign = Campaign.Create(
            campaignId,
            _workspaceId,
            Guid.NewGuid(),
            "TikTokAds",
            "ext-123",
            "Campanha TikTok Teste",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 100m).Value;

        _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new PauseCampaignCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new PauseCampaignCommand(_workspaceId, campaignId, "Alarme de CPA");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Paused);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que PauseCampaignCommandHandler falha quando a campanha não é localizada.
    /// </summary>
    [Fact]
    public async Task PauseCampaign_ShouldReturnNotFound_WhenCampaignDoesNotExist()
    {
        var campaignId = Guid.NewGuid();
        _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
            .Returns((Campaign?)null);

        var handler = new PauseCampaignCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new PauseCampaignCommand(_workspaceId, campaignId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.NotFound");
    }

    /// <summary>
    /// Valida que AdjustCampaignBudgetCommandHandler reajusta orçamento diário em percentual.
    /// </summary>
    [Fact]
    public async Task AdjustCampaignBudget_ShouldApplyPercentage_Correctly()
    {
        var campaignId = Guid.NewGuid();
        var campaign = Campaign.Create(
            campaignId,
            _workspaceId,
            Guid.NewGuid(),
            "TikTokAds",
            "ext-123",
            "Campanha TikTok",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 100m).Value;

        _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new AdjustCampaignBudgetCommandHandler(_hierarchyRepository, _unitOfWork);
        // Reduz em 20% (-20%)
        var command = new AdjustCampaignBudgetCommand(_workspaceId, campaignId, PercentageChange: -20m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        campaign.DailyBudget.Should().Be(80m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ReallocateBudgetCommandHandler transfere saldo entre a campanha de origem e destino.
    /// </summary>
    [Fact]
    public async Task ReallocateBudget_ShouldTransferBudgetBetweenCampaigns_Successfully()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var sourceCampaign = Campaign.Create(
            sourceId,
            _workspaceId,
            Guid.NewGuid(),
            "TikTokAds",
            "ext-source",
            "TikTok Origem",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 100m).Value;

        var targetCampaign = Campaign.Create(
            targetId,
            _workspaceId,
            Guid.NewGuid(),
            "GoogleAds",
            "ext-target",
            "Google Destino",
            CampaignStatus.Active,
            "Conversions",
            dailyBudget: 200m).Value;

        _hierarchyRepository.GetCampaignByIdAsync(sourceId, Arg.Any<CancellationToken>())
            .Returns(sourceCampaign);
        _hierarchyRepository.GetCampaignByIdAsync(targetId, Arg.Any<CancellationToken>())
            .Returns(targetCampaign);

        var handler = new ReallocateBudgetCommandHandler(_hierarchyRepository, _unitOfWork);
        // Transfere 20% do orçamento da origem (100 * 20% = 20) -> Origem fica 80, Destino fica 220
        var command = new ReallocateBudgetCommand(_workspaceId, sourceId, targetId, 20m, IsPercentage: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sourceCampaign.DailyBudget.Should().Be(80m);
        targetCampaign.DailyBudget.Should().Be(220m);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

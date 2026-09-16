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
/// Testes unitários para o manipulador de ativação pontual de campanha <see cref="ActivateCampaignCommandHandler"/>.
/// </summary>
public sealed class ActivateCampaignCommandHandlerTests
{
    private readonly ICampaignHierarchyRepository _hierarchyRepository = Substitute.For<ICampaignHierarchyRepository>();
    private readonly IIntegrationsUnitOfWork _unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    /// <summary>
    /// Valida que a campanha é ativada com sucesso quando existe e pertence ao workspace correto.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ActivateCampaign_ShouldSucceed_WhenCampaignExistsAndBelongsToWorkspace()
    {
        var campaignId = Guid.NewGuid();
        var campaign = Campaign.Create(
            campaignId,
            _workspaceId,
            Guid.NewGuid(),
            "MetaAds",
            "ext-activate-1",
            "Campanha Para Ativar",
            CampaignStatus.Paused,
            "Conversions",
            dailyBudget: 200m).Value;

        _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new ActivateCampaignCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new ActivateCampaignCommand(_workspaceId, campaignId, "Ativação de teste");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Active);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a ativação falha quando a campanha não é encontrada no banco de dados do inquilino.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ActivateCampaign_ShouldReturnNotFound_WhenCampaignDoesNotExist()
    {
        var campaignId = Guid.NewGuid();
        _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
            .Returns((Campaign?)null);

        var handler = new ActivateCampaignCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new ActivateCampaignCommand(_workspaceId, campaignId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.NotFound");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que a ativação falha quando a campanha pertence a outro workspace.
    /// </summary>
    /// <returns>Tarefa assíncrona de teste.</returns>
    [Fact]
    public async Task ActivateCampaign_ShouldReturnValidationFailure_WhenWorkspaceMismatch()
    {
        var campaignId = Guid.NewGuid();
        var otherWorkspaceId = Guid.NewGuid();
        var campaign = Campaign.Create(
            campaignId,
            otherWorkspaceId,
            Guid.NewGuid(),
            "MetaAds",
            "ext-activate-2",
            "Campanha Outro Workspace",
            CampaignStatus.Paused,
            "Conversions",
            dailyBudget: 150m).Value;

        _hierarchyRepository.GetCampaignByIdAsync(campaignId, Arg.Any<CancellationToken>())
            .Returns(campaign);

        var handler = new ActivateCampaignCommandHandler(_hierarchyRepository, _unitOfWork);
        var command = new ActivateCampaignCommand(_workspaceId, campaignId);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Campaign.WorkspaceMismatch");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}

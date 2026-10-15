using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Domain.Campaigns;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Integrations.Application.Campaigns.Commands.Mutations;
using Integrations.Application.Meta;
using Integrations.Application.Persistence;
using Integrations.Domain.Campaigns;
using Integrations.Domain.Meta;
using MediatR;
using NSubstitute;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>As ações existentes não podem apresentar alterações só locais como sucesso na Meta.</summary>
public sealed class MetaLegacyMutationTests
{
    /// <summary>Se a Meta rejeitar a pausa, o estado local deve permanecer inalterado.</summary>
    [Fact]
    public async Task Pause_RealMetaAccount_RequiresRemoteSuccessBeforeChangingLocalState()
    {
        var workspace = Guid.NewGuid(); var accountId = Guid.NewGuid();
        var campaign = Campaign.Create(Guid.NewGuid(), workspace, accountId, "MetaAds", "123", "Cliente", CampaignStatus.Active, "OUTCOME_TRAFFIC").Value;
        var repository = Substitute.For<ICampaignHierarchyRepository>();
        repository.GetCampaignByIdAsync(campaign.Id, Arg.Any<CancellationToken>()).Returns(campaign);
        repository.GetAccountsForSyncAsync(workspace, accountId, "MetaAds", Arg.Any<CancellationToken>())
            .Returns(new[] { ConnectedAdAccount.Create(accountId, workspace, "MetaAds", "act_11", "Cliente").Value });
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<WriteMetaResourceCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<MetaWriteResult>.Failure(Error.Forbidden("Meta.PermissionDenied", "Sem permissão.")));
        var unitOfWork = Substitute.For<IIntegrationsUnitOfWork>();
        var result = await new PauseCampaignCommandHandler(repository, unitOfWork, sender).Handle(new(workspace, campaign.Id), default);
        result.IsFailure.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Active);
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}

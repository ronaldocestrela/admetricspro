using BuildingBlocks.Application.Campaigns.Commands;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using Integrations.Application.Jobs.Commands.AcceptBulkCampaignOperation;
using NSubstitute;
using Xunit;
using ITenantUserInitiatedJobCommitter = BuildingBlocks.Application.Messaging.Persistence.ITenantUserInitiatedJobCommitter;

namespace UnitTests.Backend.Integrations.Campaigns;

/// <summary>
/// Testes de aceite assíncrono de operações em massa (Fase 8).
/// </summary>
public sealed class AcceptBulkCampaignOperationCommandTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ITenantUserInitiatedJobCommitter _committer = Substitute.For<ITenantUserInitiatedJobCommitter>();

    /// <summary>
    /// Lote com mais de cinco itens deve gravar job sem executar mutações na rede.
    /// </summary>
    [Fact]
    public async Task Handle_AcimaDoLimite_DeveGravarJob()
    {
        _committer.CommitAcceptedJobAsync(
                Arg.Any<UserInitiatedJob>(),
                Arg.Any<IntegrationOutboxMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var handler = new AcceptBulkCampaignOperationCommandHandler(
            _committer,
            TenantAccessor(),
            TimeProvider.System);

        var operations = Enumerable.Range(0, 6)
            .Select(_ => new BulkCampaignOperationItem(Guid.NewGuid(), BulkCampaignActionType.Pause))
            .ToList();

        var result = await handler.Handle(
            new AcceptBulkCampaignOperationCommand(_workspaceId, operations),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(UserInitiatedJobKind.BulkCampaignOperation);
        await _committer.Received(1).CommitAcceptedJobAsync(
            Arg.Is<UserInitiatedJob>(j => j.Status == UserInitiatedJobStatus.Accepted),
            Arg.Is<IntegrationOutboxMessage>(m =>
                m.MessageType == UserInitiatedJobIntegrationMessageTypes.BulkCampaignMutation),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Lote síncrono não deve aceitar job assíncrono.
    /// </summary>
    [Fact]
    public async Task Handle_AbaixoDoLimite_DeveFalharValidacao()
    {
        var handler = new AcceptBulkCampaignOperationCommandHandler(
            _committer,
            TenantAccessor(),
            TimeProvider.System);

        var operations = Enumerable.Range(0, 3)
            .Select(_ => new BulkCampaignOperationItem(Guid.NewGuid(), BulkCampaignActionType.Pause))
            .ToList();

        var result = await handler.Handle(
            new AcceptBulkCampaignOperationCommand(_workspaceId, operations),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BulkCampaign.BelowAsyncThreshold");
    }

    private ITenantContextAccessor TenantAccessor()
    {
        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.TenantContext = TenantContext.Create(_tenantId, "ag", TenantResolutionSource.Header);
        return accessor;
    }
}
using Analytics.Application.Jobs.Commands.AcceptGenerateReport;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Infrastructure.MultiTenancy;
using FluentAssertions;
using NSubstitute;
using Xunit;
using ITenantUserInitiatedJobCommitter = BuildingBlocks.Application.Messaging.Persistence.ITenantUserInitiatedJobCommitter;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes de aceite assíncrono de geração de relatório (Fase 8).
/// </summary>
public sealed class AcceptGenerateReportCommandTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly ITenantUserInitiatedJobCommitter _committer = Substitute.For<ITenantUserInitiatedJobCommitter>();

    /// <summary>
    /// Deve gravar job Accepted e outbox sem compilar PDF no handler de aceite.
    /// </summary>
    [Fact]
    public async Task Handle_ComTenantValido_DeveGravarJob()
    {
        _committer.CommitAcceptedJobAsync(
                Arg.Any<UserInitiatedJob>(),
                Arg.Any<IntegrationOutboxMessage>(),
                Arg.Any<CancellationToken>())
            .Returns(BuildingBlocks.Domain.Primitives.Result.Success());

        var handler = new AcceptGenerateReportCommandHandler(
            _committer,
            TenantAccessor(),
            TimeProvider.System);

        var result = await handler.Handle(
            new AcceptGenerateReportCommand(
                _workspaceId,
                "Relatório Q4",
                new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 31, 23, 59, 59, DateTimeKind.Utc),
                null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Kind.Should().Be(UserInitiatedJobKind.ReportGenerate);
        await _committer.Received(1).CommitAcceptedJobAsync(
            Arg.Is<UserInitiatedJob>(j => j.Status == UserInitiatedJobStatus.Accepted),
            Arg.Is<IntegrationOutboxMessage>(m =>
                m.MessageType == UserInitiatedJobIntegrationMessageTypes.ReportGenerate),
            Arg.Any<CancellationToken>());
    }

    private ITenantContextAccessor TenantAccessor()
    {
        var accessor = Substitute.For<ITenantContextAccessor>();
        accessor.TenantContext = TenantContext.Create(_tenantId, "ag", TenantResolutionSource.Header);
        return accessor;
    }
}
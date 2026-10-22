using BuildingBlocks.Application.Integrations;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Tenants.Application.Integrations;
using Tenants.Application.Workspaces.Repositories;

namespace UnitTests.Backend.Analytics;

/// <summary>Verifica autenticação e filtragem de carteira para o dashboard consolidado.</summary>
public sealed class AnalyticsWorkspaceAuthorizationTests
{
    /// <summary>Não retorna a carteira antes de confirmar a sessão.</summary>
    [Fact]
    public async Task WithoutSession_DoesNotReadWorkspaces()
    {
        var repository = Substitute.For<IWorkspaceRepository>();
        var handler = new GetAnalyticsWorkspaceIdsQueryHandler(Substitute.For<ICurrentUserContext>(), repository, Substitute.For<ISender>());
        (await handler.Handle(new(null), default)).Error.Type.Should().Be(ErrorType.Unauthorized);
        repository.ReceivedCalls().Should().BeEmpty();
    }

    /// <summary>Um workspace negado não deve aparecer em consultas de métricas.</summary>
    [Fact]
    public async Task SpecificWorkspace_RequiresReadPermission()
    {
        var user = Substitute.For<ICurrentUserContext>();
        user.IsAuthenticated.Returns(true); user.UserId.Returns(Guid.NewGuid());
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ValidateIntegrationWorkspaceQuery>(), Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(false));
        var handler = new GetAnalyticsWorkspaceIdsQueryHandler(user, Substitute.For<IWorkspaceRepository>(), sender);
        var workspace = Guid.NewGuid();
        (await handler.Handle(new(workspace), default)).Error.Type.Should().Be(ErrorType.Forbidden);
        await sender.Received(1).Send(Arg.Is<ValidateIntegrationWorkspaceQuery>(q => q.WorkspaceId == workspace && !q.Manage), Arg.Any<CancellationToken>());
    }
}

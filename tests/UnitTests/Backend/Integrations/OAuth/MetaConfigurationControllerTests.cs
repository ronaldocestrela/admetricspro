using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Integrations.Application.Meta;
using Integrations.Domain.Meta;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;

namespace UnitTests.Backend.Integrations.OAuth;

/// <summary>Endpoints versionados encaminham opções e leituras ao mediador sem acesso direto à persistência.</summary>
public sealed class MetaConfigurationControllerTests
{
    /// <summary>O catálogo é retornado pelo contrato da conta escolhida.</summary>
    [Fact]
    public async Task Options_ReturnsResultFromMediator()
    {
        var sender = Substitute.For<ISender>();
        var workspace = Guid.NewGuid(); var account = Guid.NewGuid();
        sender.Send(Arg.Any<GetMetaResourceOptionsQuery>(), Arg.Any<CancellationToken>()).Returns(MetaResourceCatalog.Get("campaigns"));
        var controller = new MetaResourcesController(sender, Substitute.For<IMetaOperationRepository>());
        (await controller.Options(workspace, account, "campaigns", default)).Should().BeOfType<OkObjectResult>();
        await sender.Received(1).Send(Arg.Is<GetMetaResourceOptionsQuery>(q => q.WorkspaceId == workspace && q.AccountId == account && q.Resource == "campaigns"), Arg.Any<CancellationToken>());
    }
}

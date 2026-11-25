using BackofficeApp.Components.Backoffice;
using BackofficeApp.Services;
using Bunit;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using UnitTests.Frontend.Common;
using Xunit;

namespace UnitTests.Frontend.Components.Backoffice;

/// <summary>
/// Testes do painel de dead-letters no Backoffice.
/// </summary>
public sealed class MessagingDeadLettersDashboardTests : BunitTestBase
{
    [Fact]
    public void RendersDeadLetterRows_WhenApiReturnsItems()
    {
        var client = Substitute.For<IMessagingDeadLetterClientService>();
        client.GetDeadLettersAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>.Success(
                new List<IntegrationDeadLetterListItemDto>
                {
                    new(
                        Guid.NewGuid(),
                        "catalog.tenant-welcome-email.v1",
                        null,
                        3,
                        "Email.SendFailed",
                        DateTime.UtcNow),
                }));

        Services.AddSingleton(client);

        var cut = Render<MessagingDeadLettersDashboard>();
        cut.Markup.Should().Contain("catalog.tenant-welcome-email.v1");
        cut.Markup.Should().Contain("Reprocessar");
    }
}
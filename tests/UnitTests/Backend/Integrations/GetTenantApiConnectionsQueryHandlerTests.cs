using FluentAssertions;
using Master.Application.Integrations.Queries.GetTenantApiConnections;
using Master.Application.Integrations.Repositories;
using Master.Domain.Integrations;
using NSubstitute;

namespace UnitTests.Backend.Integrations;

/// <summary>
/// Unit tests for <see cref="GetTenantApiConnectionsQueryHandler"/>.
/// </summary>
public sealed class GetTenantApiConnectionsQueryHandlerTests
{
    private readonly ITenantApiConnectionRepository _repo = Substitute.For<ITenantApiConnectionRepository>();
    private readonly GetTenantApiConnectionsQueryHandler _handler;

    /// <summary>
    /// Initializes test dependencies.
    /// </summary>
    public GetTenantApiConnectionsQueryHandlerTests()
    {
        _handler = new GetTenantApiConnectionsQueryHandler(_repo);
    }

    /// <summary>
    /// Verifies that handler queries repository and returns mapped connection DTOs with pagination.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnPagedConnectionDtos()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var connection1 = TenantApiConnection.Create(
            tenantId, "Tenant Alpha", AdPlatform.Meta, "act_123", "Main Account", now.AddDays(10), now).Value;
        var connection2 = TenantApiConnection.Create(
            tenantId, "Tenant Beta", AdPlatform.Google, "456-789-0123", "Google Ads", now.AddDays(2), now).Value;

        connection2.EvaluateExpiration(now, TimeSpan.FromDays(7));

        _repo.GetConnectionsAsync(AdPlatform.Meta, null, Arg.Any<CancellationToken>())
            .Returns(new List<TenantApiConnection> { connection1 });

        var query = new GetTenantApiConnectionsQuery(Platform: AdPlatform.Meta, PageNumber: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        var item = result.Value[0];
        item.TenantName.Should().Be("Tenant Alpha");
        item.Platform.Should().Be(AdPlatform.Meta);
        item.PlatformName.Should().Be("Meta Graph API");
        item.AccountIdentifier.Should().Be("act_123");
        item.Status.Should().Be(ApiConnectionStatus.Connected);
    }
}

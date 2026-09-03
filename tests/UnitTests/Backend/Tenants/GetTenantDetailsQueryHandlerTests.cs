using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Queries.GetTenantDetails;
using Master.Domain.Tenants;
using NSubstitute;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="GetTenantDetailsQueryHandler"/>.
/// </summary>
public sealed class GetTenantDetailsQueryHandlerTests
{
    private readonly ITenantReadOnlyRepository _readOnlyRepository = Substitute.For<ITenantReadOnlyRepository>();

    /// <summary>
    /// Verifies that handler returns a NotFound failure when tenant does not exist.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new GetTenantDetailsQueryHandler(_readOnlyRepository);
        var tenantId = TenantId.New();
        var query = new GetTenantDetailsQuery(tenantId);

        _readOnlyRepository.GetDetailsByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns((TenantDetailsResponse?)null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.NotFound");
    }

    /// <summary>
    /// Verifies that handler returns success with projected tenant details when tenant exists.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnTenantDetails_WhenTenantExists()
    {
        // Arrange
        var handler = new GetTenantDetailsQueryHandler(_readOnlyRepository);
        var tenantId = TenantId.New();
        var query = new GetTenantDetailsQuery(tenantId);
        var expectedResponse = new TenantDetailsResponse(
            tenantId.Value,
            "Beta Corp",
            "22333444000199",
            "beta",
            TenantStatus.Active.ToString(),
            SubscriptionTier.Pro.ToString(),
            DateTime.UtcNow.AddMonths(6),
            DateTime.UtcNow);

        _readOnlyRepository.GetDetailsByIdAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedResponse);
    }
}

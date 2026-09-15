using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Tenants.Application.Branding.Commands.UpdateTenantBranding;
using Tenants.Application.Branding.DTOs;
using Tenants.Application.Branding.Repositories;
using Tenants.Application.Persistence;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Unit tests for <see cref="UpdateTenantBrandingCommandHandler"/>.
/// </summary>
public sealed class UpdateTenantBrandingCommandHandlerTests
{
    private readonly ITenantBrandingRepository _brandingRepository = Substitute.For<ITenantBrandingRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly UpdateTenantBrandingCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateTenantBrandingCommandHandlerTests"/>.
    /// </summary>
    public UpdateTenantBrandingCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.IsResolved.Returns(true);
        tenantContext.TenantId.Returns(_tenantId);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new UpdateTenantBrandingCommandHandler(
            _brandingRepository,
            _unitOfWork,
            _tenantContextAccessor,
            _publisher);
    }

    /// <summary>
    /// Verifies that when branding exists, it is updated and committed.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBrandingExists_ShouldUpdateAndCommit()
    {
        // Arrange
        var existing = TenantBranding.Create(
            Guid.NewGuid(),
            "#2563EB",
            "#0F172A").Value;

        _brandingRepository.GetAsync(Arg.Any<CancellationToken>()).Returns(existing);

        var command = new UpdateTenantBrandingCommand(
            "#10B981",
            "#1E293B",
            "https://cdn.example.com/light.png",
            "https://cdn.example.com/dark.svg",
            "https://cdn.example.com/fav.ico");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrimaryColor.Should().Be("#10B981");
        result.Value.SecondaryColor.Should().Be("#1E293B");
        result.Value.LightLogoUrl.Should().Be("https://cdn.example.com/light.png");
        result.Value.DarkLogoUrl.Should().Be("https://cdn.example.com/dark.svg");
        result.Value.FaviconUrl.Should().Be("https://cdn.example.com/fav.ico");

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when branding does not exist, it is created and committed.
    /// </summary>
    [Fact]
    public async Task Handle_WhenBrandingDoesNotExist_ShouldCreateAndCommit()
    {
        // Arrange
        _brandingRepository.GetAsync(Arg.Any<CancellationToken>()).Returns((TenantBranding?)null);

        var command = new UpdateTenantBrandingCommand(
            "#4F46E5",
            "#0F172A",
            null,
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PrimaryColor.Should().Be("#4F46E5");
        result.Value.SecondaryColor.Should().Be("#0F172A");

        await _brandingRepository.Received(1).AddAsync(Arg.Any<TenantBranding>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that when logo url has invalid file extension, failure is returned.
    /// </summary>
    [Fact]
    public async Task Handle_WhenLogoUrlHasInvalidExtension_ShouldReturnFailure()
    {
        // Arrange
        _brandingRepository.GetAsync(Arg.Any<CancellationToken>()).Returns((TenantBranding?)null);

        var command = new UpdateTenantBrandingCommand(
            "#4F46E5",
            "#0F172A",
            "https://cdn.example.com/invalid.gif",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TenantBranding.InvalidLightLogoFormat");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}

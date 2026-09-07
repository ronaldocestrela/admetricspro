using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Commands.ToggleWorkspaceStatus;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o comando <see cref="ToggleWorkspaceStatusCommandHandler"/>.
/// </summary>
public sealed class ToggleWorkspaceStatusCommandHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ToggleWorkspaceStatusCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private const string ValidCpf = "123.456.789-09";

    /// <summary>
    /// Inicializa a suíte com mocks configurados.
    /// </summary>
    public ToggleWorkspaceStatusCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(_tenantId);
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new ToggleWorkspaceStatusCommandHandler(
            _workspaceRepository,
            _unitOfWork,
            _tenantContextAccessor,
            _sender);
    }

    /// <summary>
    /// Valida que desativar um workspace ativo não precisa checar cota e altera o status com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoAtivo_DeveDesativarComSucesso()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Cliente Ativo", ValidCpf, 1000m, "Varejo").Value;

        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ToggleWorkspaceStatusCommand(workspaceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspace.IsActive.Should().BeFalse();

        _workspaceRepository.Received(1).Update(workspace);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que reativar um workspace inativo valida a cota e tem sucesso se dentro do limite.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoInativoEDentroDaCota_DeveReativarComSucesso()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Cliente Inativo", ValidCpf, 1000m, "Varejo").Value;
        workspace.Deactivate();

        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(2); // Cota de 3, atualmente 2 ativos

        _sender.Send(Arg.Is<GetTenantPlanLimitsQuery>(q => q.TenantId == _tenantId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto("Starter", 3, 3, 50000m)));

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new ToggleWorkspaceStatusCommand(workspaceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspace.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Valida que reativar um workspace inativo falha se a cota máxima já estiver ocupada.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoInativoECotaPreenchida_DeveFalharComCotaExcedida()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Cliente Inativo", ValidCpf, 1000m, "Varejo").Value;
        workspace.Deactivate();

        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(3); // Cota de 3, todos ocupados

        _sender.Send(Arg.Is<GetTenantPlanLimitsQuery>(q => q.TenantId == _tenantId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto("Starter", 3, 3, 50000m)));

        var command = new ToggleWorkspaceStatusCommand(workspaceId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.QuotaExceeded");
        workspace.IsActive.Should().BeFalse();
    }
}

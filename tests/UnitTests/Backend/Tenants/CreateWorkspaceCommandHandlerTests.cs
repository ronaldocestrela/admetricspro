using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Application.Tenants.Queries.GetTenantPlanLimits;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Commands.CreateWorkspace;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o comando <see cref="CreateWorkspaceCommandHandler"/>.
/// </summary>
public sealed class CreateWorkspaceCommandHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly CreateWorkspaceCommandHandler _handler;

    private readonly Guid _tenantId = Guid.NewGuid();
    private const string ValidCpf = "123.456.789-09";

    /// <summary>
    /// Inicializa a suíte com os mocks configurados para o inquilino ativo.
    /// </summary>
    public CreateWorkspaceCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(_tenantId);
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new CreateWorkspaceCommandHandler(
            _workspaceRepository,
            _unitOfWork,
            _tenantContextAccessor,
            _sender);
    }

    /// <summary>
    /// Valida que a criação bem-sucedida persiste a entidade e retorna o ID do novo workspace.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoDadosValidosEDentroDaCota_DeveCriarWorkspaceComSucesso()
    {
        // Arrange
        var command = new CreateWorkspaceCommand("Cliente Sucesso LTDA", ValidCpf, 5000m, "E-commerce");

        _workspaceRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(1);
        _workspaceRepository.ExistsByCnpjOrCpfAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        _sender.Send(Arg.Is<GetTenantPlanLimitsQuery>(q => q.TenantId == _tenantId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto("Starter", 3, 3, 50000m)));

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _workspaceRepository.Received(1).AddAsync(Arg.Is<Workspace>(w => w.Name == "Cliente Sucesso LTDA"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao tentar criar workspace quando a cota do plano já foi atingida, retorna erro de validação.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoCotaDoPlanoAtingida_DeveRetornarFalhaDeCotaExcedida()
    {
        // Arrange
        var command = new CreateWorkspaceCommand("Cliente Excedente", ValidCpf, 5000m, "E-commerce");

        // Plano Starter permite 3, e o inquilino já possui 3 ativos
        _workspaceRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(3);

        _sender.Send(Arg.Is<GetTenantPlanLimitsQuery>(q => q.TenantId == _tenantId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto("Starter", 3, 3, 50000m)));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.QuotaExceeded");

        await _workspaceRepository.DidNotReceive().AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que inquilino Enterprise pode cadastrar workspaces sem limite de cota.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoPlanoEnterprise_DevePermitirCriacaoMesmoComMuitosWorkspaces()
    {
        // Arrange
        var command = new CreateWorkspaceCommand("Cliente Enterprise", ValidCpf, 5000m, "E-commerce");

        _workspaceRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(50);
        _workspaceRepository.ExistsByCnpjOrCpfAsync(Arg.Any<string>(), null, Arg.Any<CancellationToken>()).Returns(false);

        _sender.Send(Arg.Is<GetTenantPlanLimitsQuery>(q => q.TenantId == _tenantId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto("Enterprise", int.MaxValue, int.MaxValue, decimal.MaxValue)));

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _workspaceRepository.Received(1).AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que documento fiscal já existente no mesmo inquilino retorna conflito de duplicidade.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoDocumentoFiscalJaCadastradoNoTenant_DeveRetornarConflito()
    {
        // Arrange
        var command = new CreateWorkspaceCommand("Cliente Duplicado", ValidCpf, 5000m, "Varejo");

        _workspaceRepository.CountActiveAsync(Arg.Any<CancellationToken>()).Returns(0);
        _sender.Send(Arg.Is<GetTenantPlanLimitsQuery>(q => q.TenantId == _tenantId), Arg.Any<CancellationToken>())
            .Returns(Result<TenantPlanLimitsDto>.Success(new TenantPlanLimitsDto("Starter", 3, 3, 50000m)));

        _workspaceRepository.ExistsByCnpjOrCpfAsync("12345678909", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.CnpjOrCpfAlreadyExists");

        await _workspaceRepository.DidNotReceive().AddAsync(Arg.Any<Workspace>(), Arg.Any<CancellationToken>());
    }
}

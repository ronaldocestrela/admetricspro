using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Commands.CreateSquad;
using Tenants.Application.Squads.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o comando <see cref="CreateSquadCommandHandler"/>.
/// </summary>
public sealed class CreateSquadCommandHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly CreateSquadCommandHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    /// <summary>
    /// Inicializa os testes com o contexto de inquilino ativo pré-configurado.
    /// </summary>
    public CreateSquadCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(_tenantId);
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new CreateSquadCommandHandler(
            _squadRepository,
            _unitOfWork,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que a criação com dados válidos persiste a entidade e retorna o identificador do novo squad.
    /// </summary>
    [Fact]
    public async Task Handle_ComDadosValidosENomeUnico_DeveCriarSquadComSucesso()
    {
        // Arrange
        var command = new CreateSquadCommand("Squad Performance", "Foco em ROI e escala");
        _squadRepository.ExistsByNameAsync("Squad Performance", null, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _squadRepository.Received(1).AddAsync(Arg.Is<Squad>(s => s.Name == "Squad Performance"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que nomes duplicados no mesmo inquilino são rejeitados com erro de conflito.
    /// </summary>
    [Fact]
    public async Task Handle_ComNomeJaExistente_DeveRetornarErroDeConflito()
    {
        // Arrange
        var command = new CreateSquadCommand("Squad Branding", null);
        _squadRepository.ExistsByNameAsync("Squad Branding", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.NameAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);

        await _squadRepository.DidNotReceive().AddAsync(Arg.Any<Squad>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que requisições sem contexto de inquilino resolvido retornam erro de não autorizado.
    /// </summary>
    [Fact]
    public async Task Handle_SemTenantContextResolvido_DeveRetornarUnauthorized()
    {
        // Arrange
        var unresolvContext = Substitute.For<ITenantContext>();
        unresolvContext.IsResolved.Returns(false);
        _tenantContextAccessor.TenantContext.Returns(unresolvContext);

        var command = new CreateSquadCommand("Squad Inbound", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Unresolved");
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }
}

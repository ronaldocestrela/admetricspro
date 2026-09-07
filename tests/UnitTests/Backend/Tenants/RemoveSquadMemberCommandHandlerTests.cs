using BuildingBlocks.Application.MultiTenancy;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Squads.Commands.RemoveSquadMember;
using Tenants.Application.Squads.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para <see cref="RemoveSquadMemberCommandHandler"/>.
/// </summary>
public sealed class RemoveSquadMemberCommandHandlerTests
{
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantContextAccessor _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
    private readonly RemoveSquadMemberCommandHandler _handler;

    /// <summary>
    /// Construtor dos testes.
    /// </summary>
    public RemoveSquadMemberCommandHandlerTests()
    {
        var tenantContext = Substitute.For<ITenantContext>();
        tenantContext.TenantId.Returns(Guid.NewGuid());
        tenantContext.IsResolved.Returns(true);
        _tenantContextAccessor.TenantContext.Returns(tenantContext);

        _handler = new RemoveSquadMemberCommandHandler(
            _squadRepository,
            _unitOfWork,
            _tenantContextAccessor);
    }

    /// <summary>
    /// Valida que a remoção de um membro existente no squad é concluída com sucesso.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoMembroExiste_DeveRemoverComSucesso()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Alpha", null).Value;
        squad.AddMember(userId);

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);

        var command = new RemoveSquadMemberCommand(squadId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        squad.Members.Should().BeEmpty();

        _squadRepository.Received(1).Update(squad);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que desvincular usuário que não é membro do squad retorna NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoNaoEMembro_DeveRetornarNotFound()
    {
        // Arrange
        var squadId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var squad = Squad.Create(squadId, "Squad Alpha", null).Value;

        _squadRepository.GetByIdAsync(squadId, Arg.Any<CancellationToken>()).Returns(squad);

        var command = new RemoveSquadMemberCommand(squadId, userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Squad.MemberNotFound");
    }
}

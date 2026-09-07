using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using NSubstitute;
using Tenants.Application.Ftux.DTOs;
using Tenants.Application.Ftux.Queries.GetTenantFtuxStatus;
using Tenants.Application.Integrations.Repositories;
using Tenants.Application.Squads.Repositories;
using Tenants.Application.Users.Repositories;
using Tenants.Application.Workspaces.Repositories;

namespace UnitTests.Backend.Ftux;

/// <summary>
/// Testes unitários para o manipulador da consulta de status do FTUX (<see cref="GetTenantFtuxStatusQueryHandler"/>).
/// </summary>
public sealed class GetTenantFtuxStatusQueryHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly IConnectedAdAccountRepository _adAccountRepository = Substitute.For<IConnectedAdAccountRepository>();
    private readonly ITenantUserRepository _userRepository = Substitute.For<ITenantUserRepository>();
    private readonly ISquadRepository _squadRepository = Substitute.For<ISquadRepository>();

    private GetTenantFtuxStatusQueryHandler CreateHandler()
    {
        return new GetTenantFtuxStatusQueryHandler(
            _workspaceRepository,
            _adAccountRepository,
            _userRepository,
            _squadRepository);
    }

    /// <summary>
    /// Valida que em uma conta recém-provisionada sem workspaces, conexões ou novos membros, o progresso é 25%.
    /// </summary>
    [Fact]
    public async Task Handle_WhenOnlyProvisioned_ShouldReturn25PercentProgress()
    {
        // Arrange
        _workspaceRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Workspace>());
        _adAccountRepository.CountAsync(Arg.Any<CancellationToken>())
            .Returns(0);

        var ownerUser = TenantUser.Create(
            Guid.NewGuid(),
            "Owner Agência",
            "owner@agencia.com",
            null,
            "hash",
            TenantRole.Owner).Value;

        _userRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { ownerUser });
        _squadRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Squad>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetTenantFtuxStatusQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.IsProvisioned);
        Assert.True(result.Value.Step1Completed);
        Assert.False(result.Value.Step2Completed);
        Assert.False(result.Value.Step3Completed);
        Assert.False(result.Value.Step4Completed);
        Assert.Equal(25, result.Value.ProgressPercentage);
        Assert.False(result.Value.IsCompleted);
    }

    /// <summary>
    /// Valida que com Workspace e Conta de Anúncios criada, o progresso sobe para 75%.
    /// </summary>
    [Fact]
    public async Task Handle_WhenWorkspaceAndAdAccountExist_ShouldReturn75PercentProgress()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Cliente Alpha", "12.345.678/0001-95", 5000m, "E-commerce").Value;
        _workspaceRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { workspace });
        _adAccountRepository.CountAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var ownerUser = TenantUser.Create(
            Guid.NewGuid(),
            "Owner Agência",
            "owner@agencia.com",
            null,
            "hash",
            TenantRole.Owner).Value;

        _userRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { ownerUser });
        _squadRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Squad>());

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetTenantFtuxStatusQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Step1Completed);
        Assert.True(result.Value.Step2Completed);
        Assert.True(result.Value.Step3Completed);
        Assert.False(result.Value.Step4Completed);
        Assert.Equal(75, result.Value.ProgressPercentage);
        Assert.False(result.Value.IsCompleted);
    }

    /// <summary>
    /// Valida que quando todos os 4 passos estão completos, o progresso atinge 100% e IsCompleted é true.
    /// </summary>
    [Fact]
    public async Task Handle_WhenAllStepsCompleted_ShouldReturn100PercentAndIsCompletedTrue()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Cliente Alpha", "12.345.678/0001-95", 5000m, "E-commerce").Value;
        _workspaceRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { workspace });
        _adAccountRepository.CountAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        var ownerUser = TenantUser.Create(
            Guid.NewGuid(),
            "Owner Agência",
            "owner@agencia.com",
            null,
            "hash",
            TenantRole.Owner).Value;

        var squad = Squad.Create(Guid.NewGuid(), "Squad Performance", "Célula Alpha").Value;

        _userRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { ownerUser });
        _squadRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns(new[] { squad });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetTenantFtuxStatusQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Step1Completed);
        Assert.True(result.Value.Step2Completed);
        Assert.True(result.Value.Step3Completed);
        Assert.True(result.Value.Step4Completed);
        Assert.Equal(100, result.Value.ProgressPercentage);
        Assert.True(result.Value.IsCompleted);
    }
}

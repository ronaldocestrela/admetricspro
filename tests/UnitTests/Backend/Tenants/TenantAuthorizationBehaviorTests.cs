using System.Reflection;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Security;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Tenants.Application.Rbac.Attributes;
using Tenants.Application.Rbac.Behaviors;
using Tenants.Application.Rbac.Models;
using Tenants.Application.Rbac.Services;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Suíte de testes unitários para o pipeline behavior de autorização <see cref="TenantAuthorizationBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class TenantAuthorizationBehaviorTests
{
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IPermissionEvaluator _permissionEvaluator = Substitute.For<IPermissionEvaluator>();

    /// <summary>
    /// Comando sem atributo de permissão para teste de bypass.
    /// </summary>
    public sealed record UnprotectedCommand : ICommand<Guid>;

    /// <summary>
    /// Comando protegido que requer a permissão de gerenciar squads.
    /// </summary>
    [RequireTenantPermission(TenantPermission.ManageSquads)]
    public sealed record ProtectedSquadCommand : ICommand<Guid>;

    /// <summary>
    /// Valida que requisições sem o atributo RequireTenantPermission passam sem acionar o avaliador.
    /// </summary>
    [Fact]
    public async Task Handle_RequisicaoDesprotegida_DeveProsseguirDiretamente()
    {
        // Arrange
        var behavior = new TenantAuthorizationBehavior<UnprotectedCommand, Result<Guid>>(
            _currentUserContext,
            _permissionEvaluator);

        var expectedGuid = Guid.NewGuid();
        RequestHandlerDelegate<Result<Guid>> next = (CancellationToken _) => Task.FromResult(Result<Guid>.Success(expectedGuid));

        // Act
        var result = await behavior.Handle(new UnprotectedCommand(), next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedGuid);
        await _permissionEvaluator.DidNotReceiveWithAnyArgs().HasPermissionAsync(default, default, default, default);
    }

    /// <summary>
    /// Valida que requisições protegidas falham com erro de autenticação se não houver usuário contextual autenticado.
    /// </summary>
    [Fact]
    public async Task Handle_RequisicaoProtegidaSemUsuarioAutenticado_DeveRetornarUnauthorized()
    {
        // Arrange
        _currentUserContext.UserId.Returns((Guid?)null);

        var behavior = new TenantAuthorizationBehavior<ProtectedSquadCommand, Result<Guid>>(
            _currentUserContext,
            _permissionEvaluator);

        RequestHandlerDelegate<Result<Guid>> next = (CancellationToken _) => Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));

        // Act
        var result = await behavior.Handle(new ProtectedSquadCommand(), next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Unauthorized");
    }

    /// <summary>
    /// Valida que requisições protegidas falham com erro Forbidden se o avaliador de permissões negar o acesso.
    /// </summary>
    [Fact]
    public async Task Handle_RequisicaoProtegidaSemPermissao_DeveRetornarForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserContext.UserId.Returns(userId);

        _permissionEvaluator.HasPermissionAsync(
            userId,
            TenantPermission.ManageSquads,
            Arg.Any<PermissionContext?>(),
            Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(false));

        var behavior = new TenantAuthorizationBehavior<ProtectedSquadCommand, Result<Guid>>(
            _currentUserContext,
            _permissionEvaluator);

        RequestHandlerDelegate<Result<Guid>> next = (CancellationToken _) => Task.FromResult(Result<Guid>.Success(Guid.NewGuid()));

        // Act
        var result = await behavior.Handle(new ProtectedSquadCommand(), next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.Forbidden");
    }

    /// <summary>
    /// Valida que requisições protegidas executam o handler com sucesso quando a permissão for autorizada.
    /// </summary>
    [Fact]
    public async Task Handle_RequisicaoProtegidaComPermissao_DeveExecutarHandlerComSucesso()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserContext.UserId.Returns(userId);

        _permissionEvaluator.HasPermissionAsync(
            userId,
            TenantPermission.ManageSquads,
            Arg.Any<PermissionContext?>(),
            Arg.Any<CancellationToken>()).Returns(Result<bool>.Success(true));

        var behavior = new TenantAuthorizationBehavior<ProtectedSquadCommand, Result<Guid>>(
            _currentUserContext,
            _permissionEvaluator);

        var expectedGuid = Guid.NewGuid();
        RequestHandlerDelegate<Result<Guid>> next = (CancellationToken _) => Task.FromResult(Result<Guid>.Success(expectedGuid));

        // Act
        var result = await behavior.Handle(new ProtectedSquadCommand(), next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedGuid);
    }
}

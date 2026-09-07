using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using NSubstitute;
using Tenants.Application.Persistence;
using Tenants.Application.Workspaces.Commands.UpdateWorkspace;
using Tenants.Application.Workspaces.Repositories;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o comando <see cref="UpdateWorkspaceCommandHandler"/>.
/// </summary>
public sealed class UpdateWorkspaceCommandHandlerTests
{
    private readonly IWorkspaceRepository _workspaceRepository = Substitute.For<IWorkspaceRepository>();
    private readonly ITenantUnitOfWork _unitOfWork = Substitute.For<ITenantUnitOfWork>();
    private readonly UpdateWorkspaceCommandHandler _handler;

    private const string ValidCpf = "123.456.789-09";
    private const string ValidCnpj = "12.345.678/0001-95";

    /// <summary>
    /// Inicializa a suíte com os mocks configurados.
    /// </summary>
    public UpdateWorkspaceCommandHandlerTests()
    {
        _handler = new UpdateWorkspaceCommandHandler(_workspaceRepository, _unitOfWork);
    }

    /// <summary>
    /// Valida atualização com sucesso quando o workspace existe e o documento é válido e não conflitante.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoDadosValidos_DeveAtualizarComSucesso()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Nome Antigo", ValidCpf, 1000m, "Varejo").Value;

        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceRepository.ExistsByCnpjOrCpfAsync(Arg.Any<string>(), workspaceId, Arg.Any<CancellationToken>()).Returns(false);
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(1);

        var command = new UpdateWorkspaceCommand(workspaceId, "Novo Nome", ValidCnpj, 5000m, "SaaS");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspace.Name.Should().Be("Novo Nome");
        workspace.CnpjOrCpf.Should().Be("12345678000195");
        workspace.MonthlyAdSpendBudget.Should().Be(5000m);

        _workspaceRepository.Received(1).Update(workspace);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Valida que ao tentar atualizar um workspace inexistente, retorna erro NotFound.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoWorkspaceNaoExiste_DeveRetornarNotFound()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns((Workspace?)null);

        var command = new UpdateWorkspaceCommand(workspaceId, "Novo Nome", ValidCpf, 5000m, "SaaS");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.NotFound");
    }

    /// <summary>
    /// Valida que documento fiscal pertencente a outro workspace retorna conflito.
    /// </summary>
    [Fact]
    public async Task Handle_QuandoDocumentoPertenceAOutroWorkspace_DeveRetornarConflito()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var workspace = Workspace.Create(workspaceId, "Nome Antigo", ValidCpf, 1000m, "Varejo").Value;

        _workspaceRepository.GetByIdAsync(workspaceId, Arg.Any<CancellationToken>()).Returns(workspace);
        _workspaceRepository.ExistsByCnpjOrCpfAsync("12345678000195", workspaceId, Arg.Any<CancellationToken>()).Returns(true);

        var command = new UpdateWorkspaceCommand(workspaceId, "Novo Nome", ValidCnpj, 5000m, "SaaS");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.CnpjOrCpfAlreadyExists");
    }
}

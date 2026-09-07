using BuildingBlocks.Domain.Tenants;
using FluentAssertions;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o agregado de domínio <see cref="Workspace"/>.
/// </summary>
public sealed class WorkspaceTests
{
    private const string ValidCpf = "123.456.789-09";
    private const string ValidCnpj = "12.345.678/0001-95";

    /// <summary>
    /// Valida que a criação com dados válidos e CPF sanitiza e inicializa o workspace corretamente.
    /// </summary>
    [Fact]
    public void Create_ComDadosValidosECpf_DeveCriarWorkspaceComSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string name = "Cliente Alpha LTDA";
        const decimal budget = 5000.00m;
        const string segment = "E-commerce";

        // Act
        var result = Workspace.Create(id, name, ValidCpf, budget, segment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(id);
        result.Value.Name.Should().Be(name);
        result.Value.CnpjOrCpf.Should().Be("12345678909");
        result.Value.MonthlyAdSpendBudget.Should().Be(budget);
        result.Value.Segment.Should().Be(segment);
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.Value.UpdatedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Valida que a criação com dados válidos e CNPJ sanitiza e inicializa o workspace corretamente.
    /// </summary>
    [Fact]
    public void Create_ComDadosValidosECnpj_DeveCriarWorkspaceComSucesso()
    {
        // Arrange
        var id = Guid.NewGuid();
        const string name = "Agência Beta";
        const decimal budget = 12500.50m;
        const string segment = "SaaS B2B";

        // Act
        var result = Workspace.Create(id, name, ValidCnpj, budget, segment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CnpjOrCpf.Should().Be("12345678000195");
    }

    /// <summary>
    /// Valida que a criação com nome vazio ou em branco falha.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_ComNomeVazio_DeveRetornarFalha(string? invalidName)
    {
        // Act
        var result = Workspace.Create(Guid.NewGuid(), invalidName!, ValidCpf, 1000m, "Varejo");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.NameRequired");
    }

    /// <summary>
    /// Valida que a criação com nome superior a 150 caracteres falha.
    /// </summary>
    [Fact]
    public void Create_ComNomeMuitoLongo_DeveRetornarFalha()
    {
        // Arrange
        var longName = new string('A', 151);

        // Act
        var result = Workspace.Create(Guid.NewGuid(), longName, ValidCpf, 1000m, "Varejo");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.NameTooLong");
    }

    /// <summary>
    /// Valida que a criação com documento fiscal inválido falha.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("00000000000")]
    [InlineData("11111111111111")]
    [InlineData("documento-invalido")]
    public void Create_ComDocumentoInvalido_DeveRetornarFalha(string invalidDocument)
    {
        // Act
        var result = Workspace.Create(Guid.NewGuid(), "Cliente Teste", invalidDocument, 1000m, "Varejo");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.InvalidTaxDocument");
    }

    /// <summary>
    /// Valida que a criação com orçamento mensal negativo falha.
    /// </summary>
    [Fact]
    public void Create_ComOrcamentoNegativo_DeveRetornarFalha()
    {
        // Act
        var result = Workspace.Create(Guid.NewGuid(), "Cliente Teste", ValidCpf, -100m, "Varejo");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.InvalidBudget");
    }

    /// <summary>
    /// Valida que a atualização de dados cadastrais altera os campos e registra UpdatedAtUtc.
    /// </summary>
    [Fact]
    public void UpdateDetails_ComDadosValidos_DeveAtualizarPropriedadesEData()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Nome Antigo", ValidCpf, 1000m, "Varejo").Value;

        // Act
        var result = workspace.UpdateDetails("Novo Nome", ValidCnpj, 7500m, "Infoproduto");

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspace.Name.Should().Be("Novo Nome");
        workspace.CnpjOrCpf.Should().Be("12345678000195");
        workspace.MonthlyAdSpendBudget.Should().Be(7500m);
        workspace.Segment.Should().Be("Infoproduto");
        workspace.UpdatedAtUtc.Should().NotBeNull();
        workspace.UpdatedAtUtc!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Valida que desativar um workspace ativo altera o status com sucesso.
    /// </summary>
    [Fact]
    public void Deactivate_QuandoAtivo_DeveDesativarComSucesso()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Cliente Teste", ValidCpf, 1000m, "Varejo").Value;

        // Act
        var result = workspace.Deactivate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspace.IsActive.Should().BeFalse();
        workspace.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Valida que desativar um workspace já inativo retorna conflito.
    /// </summary>
    [Fact]
    public void Deactivate_QuandoJaInativo_DeveRetornarConflito()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Cliente Teste", ValidCpf, 1000m, "Varejo").Value;
        workspace.Deactivate();

        // Act
        var result = workspace.Deactivate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.AlreadyInactive");
    }

    /// <summary>
    /// Valida que ativar um workspace inativo altera o status com sucesso.
    /// </summary>
    [Fact]
    public void Activate_QuandoInativo_DeveAtivarComSucesso()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Cliente Teste", ValidCpf, 1000m, "Varejo").Value;
        workspace.Deactivate();

        // Act
        var result = workspace.Activate();

        // Assert
        result.IsSuccess.Should().BeTrue();
        workspace.IsActive.Should().BeTrue();
        workspace.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Valida que ativar um workspace já ativo retorna conflito.
    /// </summary>
    [Fact]
    public void Activate_QuandoJaAtivo_DeveRetornarConflito()
    {
        // Arrange
        var workspace = Workspace.Create(Guid.NewGuid(), "Cliente Teste", ValidCpf, 1000m, "Varejo").Value;

        // Act
        var result = workspace.Activate();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Workspace.AlreadyActive");
    }
}

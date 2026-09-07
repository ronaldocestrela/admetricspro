using FluentAssertions;
using Master.Application.Repositories;
using Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;
using Master.Domain.Tenants;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Tenants;

/// <summary>
/// Testes unitários para o handler de verificação de disponibilidade de documento fiscal (<see cref="CheckTaxDocumentAvailabilityQueryHandler"/>).
/// </summary>
public sealed class CheckTaxDocumentAvailabilityQueryHandlerTests
{
    private readonly ITenantRepository _tenantRepository = Substitute.For<ITenantRepository>();

    /// <summary>
    /// Valida que um CPF válido e não cadastrado é considerado disponível para novo inquilino.
    /// </summary>
    [Fact]
    public async Task Handle_WhenCpfIsValidAndNotTaken_ShouldReturnAvailable()
    {
        // Arrange
        _tenantRepository.GetByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var handler = new CheckTaxDocumentAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckTaxDocumentAvailabilityQuery("529.982.247-25");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        result.Value.DocumentType.Should().Be("CPF");
        result.Value.Document.Should().Be("52998224725");
        result.Value.FormattedDocument.Should().Be("529.982.247-25");
        result.Value.Reason.Should().BeNull();
    }

    /// <summary>
    /// Valida que um CNPJ válido e não cadastrado é considerado disponível para novo inquilino.
    /// </summary>
    [Fact]
    public async Task Handle_WhenCnpjIsValidAndNotTaken_ShouldReturnAvailable()
    {
        // Arrange
        _tenantRepository.GetByCnpjAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);

        var handler = new CheckTaxDocumentAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckTaxDocumentAvailabilityQuery("12.345.678/0001-95");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.IsAvailable.Should().BeTrue();
        result.Value.DocumentType.Should().Be("CNPJ");
        result.Value.Document.Should().Be("12345678000195");
        result.Value.FormattedDocument.Should().Be("12.345.678/0001-95");
        result.Value.Reason.Should().BeNull();
    }

    /// <summary>
    /// Valida que quando o CPF/CNPJ já existe no catálogo Master, o retorno indica indisponibilidade.
    /// </summary>
    [Fact]
    public async Task Handle_WhenDocumentAlreadyRegistered_ShouldReturnNotAvailable()
    {
        // Arrange
        var existingTenant = Tenant.Create("Agencia Existente", "12345678000195", "agencia-existente").Value;
        _tenantRepository.GetByCnpjAsync("12345678000195", Arg.Any<CancellationToken>())
            .Returns(existingTenant);

        var handler = new CheckTaxDocumentAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckTaxDocumentAvailabilityQuery("12.345.678/0001-95");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.DocumentType.Should().Be("CNPJ");
        result.Value.Reason.Should().Contain("já está em uso");
    }

    /// <summary>
    /// Valida que um documento com dígitos verificadores matematicamente inválidos é rejeitado com IsValid = false.
    /// </summary>
    /// <param name="invalidDoc">Documento fiscal inválido.</param>
    [Theory]
    [InlineData("12345678901")] // CPF com dígito inválido
    [InlineData("12345678000100")] // CNPJ com dígito inválido
    [InlineData("11111111111")] // CPF com dígitos repetidos
    [InlineData("00000000000000")] // CNPJ com dígitos repetidos
    [InlineData("123")] // Tamanho incompatível
    public async Task Handle_WhenChecksumOrLengthIsInvalid_ShouldReturnInvalidDocument(string invalidDoc)
    {
        // Arrange
        var handler = new CheckTaxDocumentAvailabilityQueryHandler(_tenantRepository);
        var query = new CheckTaxDocumentAvailabilityQuery(invalidDoc);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.IsAvailable.Should().BeFalse();
        result.Value.Reason.Should().NotBeNullOrWhiteSpace();
    }
}

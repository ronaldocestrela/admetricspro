using Analytics.Application.Currencies.Dtos;
using Analytics.Application.Currencies.Queries.ConvertCurrency;
using Analytics.Application.Currencies.Queries.ConvertCurrencyBatch;
using Analytics.Application.Taxonomy.Dtos;
using Analytics.Application.Taxonomy.Queries.BatchClassifyTaxonomy;
using Analytics.Application.Taxonomy.Queries.ClassifyTaxonomy;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using WebApi.Controllers.v1;
using WebApi.Models;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para o controlador REST <see cref="AnalyticsController"/>.
/// </summary>
public sealed class AnalyticsControllerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly AnalyticsController _controller;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="AnalyticsControllerTests"/>.
    /// </summary>
    public AnalyticsControllerTests()
    {
        _controller = new AnalyticsController(_sender);
    }

    /// <summary>
    /// Valida que a rota ConvertCurrency retorna HTTP 200 OK quando o mediador retorna sucesso.
    /// </summary>
    [Fact]
    public async Task ConvertCurrency_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var dto = new CurrencyConversionDto(100m, 545m, "USD", "BRL", 5.45m, date, DateTime.UtcNow);

        _sender.Send(Arg.Any<ConvertCurrencyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CurrencyConversionDto>.Success(dto));

        // Act
        var result = await _controller.ConvertCurrency(100m, "USD", "BRL", date);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var body = okResult.Value as Result<CurrencyConversionDto>;
        body!.IsSuccess.Should().BeTrue();
        body.Value.ConvertedAmount.Should().Be(545m);
    }

    /// <summary>
    /// Valida que a rota ConvertCurrency retorna HTTP 400 Bad Request quando a conversão falha.
    /// </summary>
    [Fact]
    public async Task ConvertCurrency_WhenFailure_ShouldReturnBadRequest()
    {
        // Arrange
        _sender.Send(Arg.Any<ConvertCurrencyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<CurrencyConversionDto>.Failure(Error.Validation("Currency.Invalid", "Moeda inválida.")));

        // Act
        var result = await _controller.ConvertCurrency(100m, "INVALID", "BRL", null);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Valida que requisição em lote com payload nulo retorna HTTP 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task ConvertCurrencyBatch_WhenRequestIsNull_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ConvertCurrencyBatch(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Valida que a conversão em lote bem-sucedida retorna HTTP 200 OK com itens convertidos.
    /// </summary>
    [Fact]
    public async Task ConvertCurrencyBatch_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var request = new ConvertCurrencyBatchApiRequest(
            Items: new[] { new CurrencyBatchItemApiRequest(100m, "USD", DateTime.UtcNow) },
            TargetCurrency: "BRL");

        var dtos = new[]
        {
            new CurrencyConversionDto(100m, 545m, "USD", "BRL", 5.45m, DateTime.UtcNow.Date, DateTime.UtcNow)
        };

        _sender.Send(Arg.Any<ConvertCurrencyBatchQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CurrencyConversionDto>>.Success(dtos));

        // Act
        var result = await _controller.ConvertCurrencyBatch(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Valida que requisição nula para classificação de taxonomia retorna HTTP 400 Bad Request.
    /// </summary>
    [Fact]
    public async Task ClassifyTaxonomy_WhenRequestIsNull_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ClassifyTaxonomy(null!);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    /// <summary>
    /// Valida que a classificação de taxonomia bem-sucedida retorna HTTP 200 OK.
    /// </summary>
    [Fact]
    public async Task ClassifyTaxonomy_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var request = new ClassifyTaxonomyApiRequest("[TOF] Prospecção Aberta", "Conjunto 1", "Vídeo 1", "ref-1");
        var dto = new TaxonomyClassificationDto(
            ReferenceId: "ref-1",
            CampaignName: "[TOF] Prospecção Aberta",
            AdSetName: "Conjunto 1",
            AdName: "Vídeo 1",
            FunnelStage: "Top",
            AudienceType: "Broad",
            CreativeType: "Video",
            Tags: new[] { "Top", "Broad", "Video" });

        _sender.Send(Arg.Any<ClassifyTaxonomyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TaxonomyClassificationDto>.Success(dto));

        // Act
        var result = await _controller.ClassifyTaxonomy(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Valida que a classificação de taxonomia em lote bem-sucedida retorna HTTP 200 OK com os itens classificados.
    /// </summary>
    [Fact]
    public async Task ClassifyTaxonomyBatch_WhenSuccessful_ShouldReturnOk()
    {
        // Arrange
        var request = new BatchClassifyTaxonomyApiRequest(
            Items: new[]
            {
                new ClassifyTaxonomyApiRequest("[BOF] Remarketing 7D", null, null, "ref-2")
            });

        var dtos = new[]
        {
            new TaxonomyClassificationDto("ref-2", "[BOF] Remarketing 7D", null, null, "Bottom", "Unknown", "Unknown", new[] { "Bottom" })
        };

        _sender.Send(Arg.Any<BatchClassifyTaxonomyQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<TaxonomyClassificationDto>>.Success(dtos));

        // Act
        var result = await _controller.ClassifyTaxonomyBatch(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }
}

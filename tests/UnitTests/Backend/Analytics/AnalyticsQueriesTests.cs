using Analytics.Application.Currencies.Dtos;
using Analytics.Application.Currencies.Queries.ConvertCurrency;
using Analytics.Application.Currencies.Queries.ConvertCurrencyBatch;
using Analytics.Application.Taxonomy.Queries.BatchClassifyTaxonomy;
using Analytics.Application.Taxonomy.Queries.ClassifyTaxonomy;
using Analytics.Domain.Currencies;
using Analytics.Domain.Taxonomy;
using BuildingBlocks.Domain.Primitives;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace UnitTests.Backend.Analytics;

/// <summary>
/// Testes unitários para os manipuladores de consultas CQRS do módulo Analytics.
/// </summary>
public sealed class AnalyticsQueriesTests
{
    private readonly ICurrencyConverter _currencyConverterMock = Substitute.For<ICurrencyConverter>();
    private readonly ITaxonomyClassifier _taxonomyClassifierMock = Substitute.For<ITaxonomyClassifier>();

    /// <summary>
    /// Valida que a consulta ConvertCurrencyQuery retorna o DTO mapeado corretamente em caso de sucesso.
    /// </summary>
    [Fact]
    public async Task ConvertCurrencyQueryHandler_WhenSuccess_ShouldReturnMappedDto()
    {
        // Arrange
        var handler = new ConvertCurrencyQueryHandler(_currencyConverterMock);
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var query = new ConvertCurrencyQuery(100m, "USD", "BRL", date);

        var conversionResult = new CurrencyConversionResult(100m, 545m, "USD", "BRL", 5.45m, date, DateTime.UtcNow);
        _currencyConverterMock.ConvertAsync(100m, "USD", "BRL", date, Arg.Any<CancellationToken>())
            .Returns(Result<CurrencyConversionResult>.Success(conversionResult));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OriginalAmount.Should().Be(100m);
        result.Value.ConvertedAmount.Should().Be(545m);
        result.Value.SourceCurrency.Should().Be("USD");
        result.Value.TargetCurrency.Should().Be("BRL");
        result.Value.ExchangeRate.Should().Be(5.45m);
    }

    /// <summary>
    /// Valida que a consulta ConvertCurrencyBatchQuery converte os múltiplos itens solicitados.
    /// </summary>
    [Fact]
    public async Task ConvertCurrencyBatchQueryHandler_WhenSuccess_ShouldReturnListDto()
    {
        // Arrange
        var handler = new ConvertCurrencyBatchQueryHandler(_currencyConverterMock);
        var date = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);
        var items = new[]
        {
            new CurrencyBatchItemInput(100m, "USD", date),
            new CurrencyBatchItemInput(50m, "EUR", date)
        };
        var query = new ConvertCurrencyBatchQuery(items, "BRL");

        var conversionResults = new[]
        {
            new CurrencyConversionResult(100m, 545m, "USD", "BRL", 5.45m, date, DateTime.UtcNow),
            new CurrencyConversionResult(50m, 297.5m, "EUR", "BRL", 5.95m, date, DateTime.UtcNow)
        };

        _currencyConverterMock.ConvertBatchAsync(Arg.Any<IEnumerable<CurrencyConversionRequest>>(), "BRL", Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<CurrencyConversionResult>>.Success(conversionResults));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].ConvertedAmount.Should().Be(545m);
        result.Value[1].ConvertedAmount.Should().Be(297.5m);
    }

    /// <summary>
    /// Valida que a consulta ClassifyTaxonomyQuery retorna os estágios e tags mapeados no DTO.
    /// </summary>
    [Fact]
    public async Task ClassifyTaxonomyQueryHandler_WhenSuccess_ShouldReturnMappedDto()
    {
        // Arrange
        var handler = new ClassifyTaxonomyQueryHandler(_taxonomyClassifierMock);
        var query = new ClassifyTaxonomyQuery("[TOF] Campanha Prospecção", "Conjunto 1", "Vídeo 1", "ref-100");

        var classificationResult = new TaxonomyClassificationResult
        {
            ReferenceId = "ref-100",
            CampaignName = "[TOF] Campanha Prospecção",
            AdSetName = "Conjunto 1",
            AdName = "Vídeo 1",
            FunnelStage = FunnelStage.Top,
            AudienceType = AudienceType.Broad,
            CreativeType = CreativeType.Video,
            Tags = new[] { "Top", "Broad", "Video" }
        };

        _taxonomyClassifierMock.Classify("[TOF] Campanha Prospecção", "Conjunto 1", "Vídeo 1", "ref-100")
            .Returns(Result<TaxonomyClassificationResult>.Success(classificationResult));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReferenceId.Should().Be("ref-100");
        result.Value.FunnelStage.Should().Be("Top");
        result.Value.AudienceType.Should().Be("Broad");
        result.Value.CreativeType.Should().Be("Video");
        result.Value.Tags.Should().Contain("Top");
    }

    /// <summary>
    /// Valida que a consulta BatchClassifyTaxonomyQuery processa e mapeia múltiplos itens.
    /// </summary>
    [Fact]
    public async Task BatchClassifyTaxonomyQueryHandler_WhenSuccess_ShouldReturnMultipleResults()
    {
        // Arrange
        var handler = new BatchClassifyTaxonomyQueryHandler(_taxonomyClassifierMock);
        var items = new[]
        {
            new BatchClassifyTaxonomyItem("[TOF] Campanha 1", null, null, "1"),
            new BatchClassifyTaxonomyItem("[BOF] Campanha 2", null, null, "2")
        };
        var query = new BatchClassifyTaxonomyQuery(items);

        var batchResults = new[]
        {
            new TaxonomyClassificationResult
            {
                ReferenceId = "1",
                CampaignName = "[TOF] Campanha 1",
                FunnelStage = FunnelStage.Top
            },
            new TaxonomyClassificationResult
            {
                ReferenceId = "2",
                CampaignName = "[BOF] Campanha 2",
                FunnelStage = FunnelStage.Bottom
            }
        };

        _taxonomyClassifierMock.ClassifyBatch(Arg.Any<IEnumerable<CampaignTaxonomyInput>>())
            .Returns(Result<IReadOnlyList<TaxonomyClassificationResult>>.Success(batchResults));

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].FunnelStage.Should().Be("Top");
        result.Value[1].FunnelStage.Should().Be("Bottom");
    }
}

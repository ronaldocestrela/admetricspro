using Analytics.Domain.Attribution;

namespace Analytics.Application.Attribution.Dtos;

/// <summary>
/// Entrada representando um ponto de contato de marketing na jornada de conversão.
/// </summary>
/// <param name="Channel">Nome da plataforma ou canal de marketing (ex: Meta, Google, TikTok, Bing).</param>
/// <param name="CampaignName">Nome descritivo da campanha de anúncio (opcional).</param>
/// <param name="OccurredAtUtc">Data e hora do ponto de contato em UTC.</param>
/// <param name="TouchType">Tipo da interação (Click ou Impression).</param>
/// <param name="Cost">Custo do ponto de contato individual (se disponível).</param>
public sealed record AttributionTouchpointInput(
    string Channel,
    string? CampaignName,
    DateTime OccurredAtUtc,
    TouchpointType TouchType = TouchpointType.Click,
    decimal Cost = 0m);

/// <summary>
/// Entrada representando uma jornada completa de conversão com seus respectivos pontos de contato.
/// </summary>
/// <param name="JourneyId">Identificador único da transação ou jornada.</param>
/// <param name="CustomerId">Identificador do consumidor ou lead (opcional).</param>
/// <param name="ConvertedAtUtc">Data e hora UTC em que a conversão foi realizada.</param>
/// <param name="ConversionValue">Valor financeiro monetário gerado pela conversão.</param>
/// <param name="Touchpoints">Lista de toques cronológicos prévios à conversão.</param>
public sealed record ConversionJourneyInput(
    string JourneyId,
    string? CustomerId,
    DateTime ConvertedAtUtc,
    decimal ConversionValue,
    IReadOnlyList<AttributionTouchpointInput> Touchpoints);

/// <summary>
/// Detalhamento dos créditos de conversão, receita e eficiência de um canal sob um modelo de atribuição.
/// </summary>
/// <param name="Channel">Nome da plataforma ou canal de mídia.</param>
/// <param name="AttributedConversions">Volume ponderado de conversões atribuídas.</param>
/// <param name="AttributedRevenue">Receita monetária bruta atribuída.</param>
/// <param name="TotalTouchpoints">Total de vezes que este canal foi acionado nas jornadas.</param>
/// <param name="FirstTouchCount">Quantidade de jornadas em que o canal foi o primeiro ponto de contato.</param>
/// <param name="LastTouchCount">Quantidade de jornadas em que o canal foi o último ponto de contato.</param>
/// <param name="AssistedConversionsCount">Quantidade de jornadas que o canal influenciou como intermediário sem ter sido o último toque.</param>
/// <param name="AttributedRoas">ROAS atribuído (Receita Atribuída / Custo do Canal, se informado).</param>
/// <param name="AttributedCpa">CPA atribuído (Custo do Canal / Conversões Atribuídas, se informado).</param>
public sealed record ChannelAttributionDto(
    string Channel,
    decimal AttributedConversions,
    decimal AttributedRevenue,
    int TotalTouchpoints,
    int FirstTouchCount,
    int LastTouchCount,
    int AssistedConversionsCount,
    decimal? AttributedRoas,
    decimal? AttributedCpa);

/// <summary>
/// Resultado analítico consolidado e comparativo dos três modelos de atribuição (First-Touch, Last-Touch e Linear).
/// </summary>
/// <param name="FirstTouchChannels">Detalhamento dos canais sob o modelo de Primeiro Clique.</param>
/// <param name="LastTouchChannels">Detalhamento dos canais sob o modelo de Último Clique.</param>
/// <param name="LinearChannels">Detalhamento dos canais sob o modelo Linear.</param>
/// <param name="TotalJourneys">Quantidade total de jornadas de conversão analisadas.</param>
/// <param name="TotalConversions">Total absoluto de conversões processadas.</param>
/// <param name="TotalConversionValue">Valor financeiro total consolidado de todas as conversões.</param>
public sealed record AttributionComparisonDto(
    IReadOnlyList<ChannelAttributionDto> FirstTouchChannels,
    IReadOnlyList<ChannelAttributionDto> LastTouchChannels,
    IReadOnlyList<ChannelAttributionDto> LinearChannels,
    int TotalJourneys,
    decimal TotalConversions,
    decimal TotalConversionValue);

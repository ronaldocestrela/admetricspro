namespace Analytics.Domain.Attribution;

/// <summary>
/// Modelos matemáticos de atribuição multi-canal suportados pela plataforma.
/// </summary>
public enum AttributionModelType
{
    /// <summary>
    /// Modelo de Primeiro Clique (First-Touch): 100% do crédito é atribuído ao primeiro canal da jornada.
    /// </summary>
    FirstTouch = 1,

    /// <summary>
    /// Modelo de Último Clique (Last-Touch): 100% do crédito é atribuído ao canal imediatamente anterior à conversão.
    /// </summary>
    LastTouch = 2,

    /// <summary>
    /// Modelo Linear: O crédito é distribuído equitativamente (1/N) entre todos os canais tocados na jornada.
    /// </summary>
    Linear = 3
}

/// <summary>
/// Tipo de ponto de contato registrado na jornada do usuário.
/// </summary>
public enum TouchpointType
{
    /// <summary>
    /// Interação direta de clique no anúncio ou link.
    /// </summary>
    Click = 1,

    /// <summary>
    /// Visualização/impressão do criativo sem clique imediato (view-through).
    /// </summary>
    Impression = 2
}

/// <summary>
/// Representa um ponto de contato individual na jornada de um consumidor com uma rede de anúncios.
/// </summary>
/// <param name="Channel">Nome ou identificador da plataforma/canal (ex: Meta, Google, TikTok, Bing, Organic, Email).</param>
/// <param name="CampaignName">Nome descritivo da campanha de mídia.</param>
/// <param name="OccurredAtUtc">Data e hora em UTC em que o toque ocorreu.</param>
/// <param name="TouchType">Tipo da interação (Click ou Impression).</param>
/// <param name="Cost">Custo atribuído à interação individual (se disponível).</param>
public sealed record AttributionTouchpoint(
    string Channel,
    string? CampaignName,
    DateTime OccurredAtUtc,
    TouchpointType TouchType = TouchpointType.Click,
    decimal Cost = 0m);

/// <summary>
/// Representa uma jornada completa de conversão percorrida por um cliente ou transação.
/// </summary>
/// <param name="JourneyId">Identificador único da jornada ou transação.</param>
/// <param name="CustomerId">Identificador único ou pseudônimo do cliente/lead (opcional).</param>
/// <param name="ConvertedAtUtc">Data e hora exata em UTC em que a conversão foi realizada.</param>
/// <param name="ConversionValue">Valor financeiro monetário gerado por esta conversão.</param>
/// <param name="Touchpoints">Lista ordenada ou cronológica de pontos de contato anteriores à conversão.</param>
public sealed record ConversionJourney(
    string JourneyId,
    string? CustomerId,
    DateTime ConvertedAtUtc,
    decimal ConversionValue,
    IReadOnlyList<AttributionTouchpoint> Touchpoints);

/// <summary>
/// Resultado analítico consolidado do crédito de atribuição para um canal específico.
/// </summary>
/// <param name="Channel">Nome da plataforma ou canal de marketing.</param>
/// <param name="AttributedConversions">Volume de conversões ponderadas atribuídas a este canal.</param>
/// <param name="AttributedRevenue">Receita monetária bruta atribuída a este canal.</param>
/// <param name="TotalTouchpoints">Total de vezes que este canal apareceu nas jornadas analisadas.</param>
/// <param name="FirstTouchCount">Quantidade de jornadas em que este canal foi o primeiro ponto de contato.</param>
/// <param name="LastTouchCount">Quantidade de jornadas em que este canal foi o último ponto de contato.</param>
/// <param name="AssistedConversionsCount">Quantidade de jornadas em que o canal participou ativamente como influenciador (ponto intermediário) sem ter sido o último toque.</param>
/// <param name="AttributedRoas">ROAS atribuído (Receita Atribuída / Custo do Canal, se custo for fornecido).</param>
/// <param name="AttributedCpa">CPA atribuído (Custo do Canal / Conversões Atribuídas, se custo for fornecido).</param>
public sealed record ChannelAttributionResult(
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
/// Resultado comparativo side-by-side de múltiplos modelos de atribuição (First-Touch, Last-Touch e Linear).
/// </summary>
/// <param name="FirstTouchChannels">Detalhamento dos canais sob o modelo de Primeiro Clique.</param>
/// <param name="LastTouchChannels">Detalhamento dos canais sob o modelo de Último Clique.</param>
/// <param name="LinearChannels">Detalhamento dos canais sob o modelo Linear.</param>
/// <param name="TotalJourneys">Quantidade total de jornadas de conversão analisadas.</param>
/// <param name="TotalConversions">Total absoluto de conversões processadas.</param>
/// <param name="TotalConversionValue">Valor financeiro total consolidado de todas as conversões analisadas.</param>
public sealed record AttributionComparisonResult(
    IReadOnlyList<ChannelAttributionResult> FirstTouchChannels,
    IReadOnlyList<ChannelAttributionResult> LastTouchChannels,
    IReadOnlyList<ChannelAttributionResult> LinearChannels,
    int TotalJourneys,
    decimal TotalConversions,
    decimal TotalConversionValue);

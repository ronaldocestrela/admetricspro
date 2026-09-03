using Master.Domain.Integrations;

namespace WebApi.Models;

/// <summary>
/// Modelo de requisição HTTP para registro de consumo de cotas de APIs.
/// </summary>
/// <param name="Platform">Plataforma de anúncios.</param>
/// <param name="Units">Volume de requisições ou operações consumidas.</param>
/// <param name="TimestampUtc">Data e hora em UTC da operação (opcional).</param>
public sealed record RecordUsageApiRequest(
    AdPlatform Platform,
    long Units,
    DateTime? TimestampUtc = null);

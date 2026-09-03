namespace WebApi.Models;

/// <summary>
/// Representa os dados de status e integridade operacional da API.
/// </summary>
/// <param name="Status">Estado de funcionamento do serviço (ex.: Healthy).</param>
/// <param name="TimestampUtc">Momento em UTC em que a verificação de saúde foi executada.</param>
/// <param name="Service">Nome identificador do serviço ou aplicação.</param>
/// <param name="Environment">Nome do ambiente de execução (ex.: Development, Staging, Production).</param>
public sealed record HealthStatusResponse(
    string Status,
    DateTime TimestampUtc,
    string Service,
    string Environment);

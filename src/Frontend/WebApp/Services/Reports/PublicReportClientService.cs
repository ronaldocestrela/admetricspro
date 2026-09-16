using System.Net.Http.Json;
using System.Text.Json;
using Analytics.Application.Reports.DTOs;
using BuildingBlocks.Domain.Primitives;

namespace WebApp.Services.Reports;

/// <summary>
/// Implementação concreta de <see cref="IPublicReportClientService"/> consumindo endpoints públicos anônimos da Web API.
/// </summary>
public sealed class PublicReportClientService : IPublicReportClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="PublicReportClientService"/>.
    /// </summary>
    public PublicReportClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<PublicSharedReportDto>> GetSharedReportAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<PublicSharedReportDto>.Failure(Error.Validation("Report.InvalidToken", "Token do relatório não informado."));
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/public/reports/{Uri.EscapeDataString(token)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<PublicSharedReportDto>.Failure(Error.NotFound("Report.NotFound", "Relatório não encontrado ou link expirado."));
            }

            var result = await response.Content.ReadFromJsonAsync<Result<PublicSharedReportDto>>(JsonOptions, cancellationToken);
            return result ?? Result<PublicSharedReportDto>.Failure(Error.Failure("Http.EmptyResponse", "Resposta vazia da API."));
        }
        catch (Exception ex)
        {
            return Result<PublicSharedReportDto>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> DownloadSharedReportPdfAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result<byte[]>.Failure(Error.Validation("Report.InvalidToken", "Token do relatório não informado."));
        }

        try
        {
            var response = await _httpClient.GetAsync($"/api/v1/public/reports/{Uri.EscapeDataString(token)}/pdf", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<byte[]>.Failure(Error.NotFound("Report.NotFound", "Arquivo PDF não encontrado."));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            return Result<byte[]>.Failure(Error.Failure("Http.RequestFailed", ex.Message));
        }
    }
}

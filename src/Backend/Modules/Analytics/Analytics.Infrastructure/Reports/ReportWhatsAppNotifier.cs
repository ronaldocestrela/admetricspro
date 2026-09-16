using System.Net.Http.Json;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Analytics.Infrastructure.Reports;

/// <summary>
/// Despachador de notificações de relatório executivo via WhatsApp utilizando webhook corporativo.
/// </summary>
public sealed class ReportWhatsAppNotifier : IReportWhatsAppNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReportWhatsAppNotifier> _logger;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReportWhatsAppNotifier"/>.
    /// </summary>
    public ReportWhatsAppNotifier(HttpClient httpClient, ILogger<ReportWhatsAppNotifier> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Result> SendReportMessageAsync(ReportWhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        if (message is null)
            return Result.Failure(Error.Validation("WhatsApp.NullMessage", "A mensagem de relatório é obrigatória."));

        if (string.IsNullOrWhiteSpace(message.RecipientPhone))
            return Result.Failure(Error.Validation("WhatsApp.InvalidPhone", "Telefone do WhatsApp não informado."));

        var text = $"*[{message.AgencyName}] Relatório Executivo - {message.WorkspaceName}*\n" +
                   $"Período: {message.PeriodDescription}\n" +
                   $"Investimento: R$ {message.TotalSpend:N2}\n" +
                   $"ROAS Blended: {message.BlendedRoas:N2}x\n" +
                   $"Conversões: {message.TotalConversions}\n\n" +
                   $"Acesse o relatório completo interativo em:\n{message.InteractiveReportUrl}";

        var payload = new
        {
            phone = message.RecipientPhone,
            message = text,
            client = message.ClientName,
            agency = message.AgencyName
        };

        try
        {
            // Se a base URL não estiver configurada no HttpClient, loga e considera despachado (modo simulado em dev/testes)
            if (_httpClient.BaseAddress is null)
            {
                _logger.LogInformation("WhatsApp report simulado para {Phone}: {Text}", message.RecipientPhone, text);
                return Result.Success();
            }

            var response = await _httpClient.PostAsJsonAsync("/api/whatsapp/send-report", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Falha ao despachar WhatsApp para {Phone}. Status {Status}: {Error}", message.RecipientPhone, response.StatusCode, errorText);
                return Result.Failure(Error.Failure("WhatsApp.DeliveryFailed", $"Falha no gateway WhatsApp: {response.StatusCode}"));
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro de conexão ao enviar WhatsApp de relatório para {Phone}", message.RecipientPhone);
            return Result.Failure(Error.Failure("WhatsApp.ConnectionError", $"Erro no envio WhatsApp: {ex.Message}"));
        }
    }
}

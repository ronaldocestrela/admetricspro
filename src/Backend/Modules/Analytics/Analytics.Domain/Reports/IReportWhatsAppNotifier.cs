using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Domain.Reports;

/// <summary>
/// Mensagem formatada enviada ao WhatsApp comercial de um cliente/stakeholder.
/// </summary>
public sealed record ReportWhatsAppMessage(
    string RecipientPhone,
    string ClientName,
    string AgencyName,
    string WorkspaceName,
    string PeriodDescription,
    decimal TotalSpend,
    decimal BlendedRoas,
    int TotalConversions,
    string? InteractiveReportUrl);

/// <summary>
/// Contrato de integração para envio de alertas e resumos de relatórios via WhatsApp.
/// </summary>
public interface IReportWhatsAppNotifier
{
    /// <summary>
    /// Despacha uma mensagem executiva formatada para o número de WhatsApp do destinatário.
    /// </summary>
    /// <param name="message">Dados da mensagem do relatório.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com sucesso ou erro de envio.</returns>
    Task<Result> SendReportMessageAsync(ReportWhatsAppMessage message, CancellationToken cancellationToken = default);
}

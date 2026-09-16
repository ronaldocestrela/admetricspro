using System.Text;
using Analytics.Domain.Reports;
using BuildingBlocks.Application.Emails;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Reports;

namespace Analytics.Infrastructure.Reports;

/// <summary>
/// Implementação do serviço orquestrador de envio de relatórios multi-canal (E-mail e WhatsApp).
/// </summary>
public sealed class ReportDispatchService : IReportDispatchService
{
    private readonly IEmailSender _emailSender;
    private readonly IReportWhatsAppNotifier _whatsAppNotifier;
    private readonly IReportRepository _repository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="ReportDispatchService"/>.
    /// </summary>
    public ReportDispatchService(
        IEmailSender emailSender,
        IReportWhatsAppNotifier whatsAppNotifier,
        IReportRepository repository)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _whatsAppNotifier = whatsAppNotifier ?? throw new ArgumentNullException(nameof(whatsAppNotifier));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ReportDispatchLog>>> DispatchReportAsync(
        GeneratedReport report,
        ReportSchedule schedule,
        ReportRenderModel model,
        CancellationToken cancellationToken = default)
    {
        if (report is null)
            return Result<IReadOnlyList<ReportDispatchLog>>.Failure(Error.Validation("ReportDispatch.NullReport", "O relatório gerado é obrigatório."));

        if (schedule is null)
            return Result<IReadOnlyList<ReportDispatchLog>>.Failure(Error.Validation("ReportDispatch.NullSchedule", "O agendamento do relatório é obrigatório."));

        if (model is null)
            return Result<IReadOnlyList<ReportDispatchLog>>.Failure(Error.Validation("ReportDispatch.NullModel", "O modelo de renderização é obrigatório."));

        var logs = new List<ReportDispatchLog>();
        var agencyName = string.IsNullOrWhiteSpace(model.Branding?.AgencyName) ? "Agência de Performance" : model.Branding.AgencyName;

        foreach (var recipient in schedule.Recipients)
        {
            if (recipient.Channel == ReportDeliveryChannel.Email)
            {
                var emailLog = await SendEmailReportAsync(report, recipient, model, agencyName, cancellationToken);
                logs.Add(emailLog);
            }
            else if (recipient.Channel == ReportDeliveryChannel.WhatsApp)
            {
                var whatsappLog = await SendWhatsAppReportAsync(report, recipient, model, agencyName, cancellationToken);
                logs.Add(whatsappLog);
            }
        }

        // Persiste os logs de auditoria
        if (logs.Count > 0)
        {
            await _repository.AddDispatchLogsAsync(logs, cancellationToken);
        }

        // Atualiza status do relatório
        report.MarkDispatched();
        await _repository.UpdateGeneratedReportAsync(report, cancellationToken);

        return Result<IReadOnlyList<ReportDispatchLog>>.Success(logs);
    }

    private async Task<ReportDispatchLog> SendEmailReportAsync(
        GeneratedReport report,
        ReportRecipient recipient,
        ReportRenderModel model,
        string agencyName,
        CancellationToken cancellationToken)
    {
        var subject = $"[Relatório Executivo] {model.ReportTitle} - {model.WorkspaceName}";
        var htmlBody = BuildEmailHtml(model, agencyName, recipient.Name);
        var textBody = $"Olá {recipient.Name},\n\nSegue o resumo do seu relatório executivo:\nInvestimento: R$ {model.KpiSummary.TotalSpend:N2}\nReceita: R$ {model.KpiSummary.TotalRevenue:N2}\nROAS: {model.KpiSummary.BlendedRoas:N2}x\nConversões: {model.KpiSummary.TotalConversions}\n\nAcesse a versão interativa completa em: {model.ShareUrl}\n\nAtenciosamente,\n{agencyName}";

        var emailResult = EmailMessage.Create(
            recipient.Destination,
            subject,
            htmlBody,
            textBody,
            model.Branding?.SupportEmail,
            agencyName);

        if (emailResult.IsFailure)
        {
            return ReportDispatchLog.CreateFailure(
                Guid.NewGuid(),
                report.Id,
                ReportDeliveryChannel.Email,
                recipient.Destination,
                DateTime.UtcNow,
                emailResult.Error.Description).Value;
        }

        var sendResult = await _emailSender.SendEmailAsync(emailResult.Value, cancellationToken);

        if (sendResult.IsFailure)
        {
            return ReportDispatchLog.CreateFailure(
                Guid.NewGuid(),
                report.Id,
                ReportDeliveryChannel.Email,
                recipient.Destination,
                DateTime.UtcNow,
                sendResult.Error.Description).Value;
        }

        return ReportDispatchLog.CreateSuccess(
            Guid.NewGuid(),
            report.Id,
            ReportDeliveryChannel.Email,
            recipient.Destination,
            DateTime.UtcNow).Value;
    }

    private async Task<ReportDispatchLog> SendWhatsAppReportAsync(
        GeneratedReport report,
        ReportRecipient recipient,
        ReportRenderModel model,
        string agencyName,
        CancellationToken cancellationToken)
    {
        var message = new ReportWhatsAppMessage(
            RecipientPhone: recipient.Destination,
            ClientName: recipient.Name,
            AgencyName: agencyName,
            WorkspaceName: model.WorkspaceName,
            PeriodDescription: $"{model.DateRangeStart:dd/MM/yyyy} a {model.DateRangeEnd:dd/MM/yyyy}",
            TotalSpend: model.KpiSummary.TotalSpend,
            BlendedRoas: model.KpiSummary.BlendedRoas,
            TotalConversions: model.KpiSummary.TotalConversions,
            InteractiveReportUrl: model.ShareUrl);

        var sendResult = await _whatsAppNotifier.SendReportMessageAsync(message, cancellationToken);

        if (sendResult.IsFailure)
        {
            return ReportDispatchLog.CreateFailure(
                Guid.NewGuid(),
                report.Id,
                ReportDeliveryChannel.WhatsApp,
                recipient.Destination,
                DateTime.UtcNow,
                sendResult.Error.Description).Value;
        }

        return ReportDispatchLog.CreateSuccess(
            Guid.NewGuid(),
            report.Id,
            ReportDeliveryChannel.WhatsApp,
            recipient.Destination,
            DateTime.UtcNow).Value;
    }

    private static string BuildEmailHtml(ReportRenderModel model, string agencyName, string clientName)
    {
        var primaryColor = model.Branding?.PrimaryColor ?? "#2563EB";
        var secondaryColor = model.Branding?.SecondaryColor ?? "#0F172A";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
        sb.AppendLine($"body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f8fafc; color: {secondaryColor}; margin: 0; padding: 20px; }}");
        sb.AppendLine(".card { background: #ffffff; border-radius: 8px; padding: 24px; max-width: 600px; margin: 0 auto; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }");
        sb.AppendLine($".header {{ background: {primaryColor}; color: #ffffff; padding: 20px; border-radius: 6px; text-align: center; }}");
        sb.AppendLine(".kpi-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin: 20px 0; }");
        sb.AppendLine(".kpi-box { background: #f1f5f9; padding: 14px; border-radius: 6px; text-align: center; }");
        sb.AppendLine(".kpi-value { font-size: 18px; font-weight: bold; }");
        sb.AppendLine(".btn { display: inline-block; padding: 12px 24px; background: " + primaryColor + "; color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: bold; margin-top: 15px; }");
        sb.AppendLine(".footer { font-size: 12px; color: #64748b; margin-top: 25px; border-top: 1px solid #e2e8f0; padding-top: 15px; text-align: center; }");
        sb.AppendLine("</style></head><body><div class='card'>");

        sb.AppendLine($"<div class='header'><h2>{agencyName}</h2><p style='margin:0;'>{model.ReportTitle} - {model.WorkspaceName}</p></div>");
        sb.AppendLine($"<p>Olá <strong>{clientName}</strong>,</p>");
        sb.AppendLine($"<p>Segue a consolidação executiva dos resultados de mídia de <strong>{model.DateRangeStart:dd/MM/yyyy}</strong> a <strong>{model.DateRangeEnd:dd/MM/yyyy}</strong>:</p>");

        sb.AppendLine("<div class='kpi-grid'>");
        sb.AppendLine($"<div class='kpi-box'><div>Investimento</div><div class='kpi-value'>R$ {model.KpiSummary.TotalSpend:N2}</div></div>");
        sb.AppendLine($"<div class='kpi-box'><div>Receita</div><div class='kpi-value'>R$ {model.KpiSummary.TotalRevenue:N2}</div></div>");
        sb.AppendLine($"<div class='kpi-box'><div>ROAS Blended</div><div class='kpi-value'>{model.KpiSummary.BlendedRoas:N2}x</div></div>");
        sb.AppendLine($"<div class='kpi-box'><div>Conversões</div><div class='kpi-value'>{model.KpiSummary.TotalConversions}</div></div>");
        sb.AppendLine("</div>");

        if (!string.IsNullOrWhiteSpace(model.CustomNotes))
        {
            sb.AppendLine($"<div style='background: #f8fafc; border-left: 4px solid {primaryColor}; padding: 10px; margin: 15px 0;'><strong>Nota da Gestão:</strong><br/>{model.CustomNotes}</div>");
        }

        if (!string.IsNullOrWhiteSpace(model.ShareUrl))
        {
            sb.AppendLine($"<div style='text-align: center;'><a href='{model.ShareUrl}' class='btn' target='_blank'>Visualizar Relatório Interativo</a></div>");
        }

        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"<p>{agencyName}");
        if (!string.IsNullOrWhiteSpace(model.Branding?.SupportEmail))
            sb.AppendLine($" | {model.Branding.SupportEmail}");
        if (!string.IsNullOrWhiteSpace(model.Branding?.SupportPhone))
            sb.AppendLine($" | {model.Branding.SupportPhone}");
        sb.AppendLine("</p></div>");

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }
}

using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Reports;

/// <summary>
/// Registra o histórico e auditoria imutável do envio de um relatório gerado a um destinatário.
/// </summary>
public sealed class ReportDispatchLog : Entity<Guid>
{
    /// <summary>
    /// Construtor privado para o EF Core.
    /// </summary>
    private ReportDispatchLog() : base(Guid.Empty)
    {
        Recipient = string.Empty;
    }

    /// <summary>
    /// Construtor privado para criação controlada via fábrica de domínio.
    /// </summary>
    private ReportDispatchLog(
        Guid id,
        Guid generatedReportId,
        ReportDeliveryChannel channel,
        string recipient,
        DateTime dispatchedAtUtc,
        bool isSuccess,
        string? errorMessage)
        : base(id)
    {
        GeneratedReportId = generatedReportId;
        Channel = channel;
        Recipient = recipient;
        DispatchedAtUtc = dispatchedAtUtc;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Identificador do relatório gerado associado ao despacho.
    /// </summary>
    public Guid GeneratedReportId { get; private set; }

    /// <summary>
    /// Canal de entrega utilizado (E-mail ou WhatsApp).
    /// </summary>
    public ReportDeliveryChannel Channel { get; private set; }

    /// <summary>
    /// Destinatário receptor (e-mail ou número de telefone).
    /// </summary>
    public string Recipient { get; private set; }

    /// <summary>
    /// Data e hora UTC da tentativa de envio.
    /// </summary>
    public DateTime DispatchedAtUtc { get; private set; }

    /// <summary>
    /// Indica se o envio foi concluído com êxito pelo canal.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Mensagem descritiva de erro capturada caso o envio tenha falhado.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Cria um registro de auditoria de despacho de relatório com sucesso.
    /// </summary>
    public static Result<ReportDispatchLog> CreateSuccess(
        Guid id,
        Guid generatedReportId,
        ReportDeliveryChannel channel,
        string recipient,
        DateTime dispatchedAtUtc)
    {
        if (id == Guid.Empty)
            return Result<ReportDispatchLog>.Failure(Error.Validation("ReportDispatchLog.EmptyId", "O identificador do log é obrigatório."));

        if (generatedReportId == Guid.Empty)
            return Result<ReportDispatchLog>.Failure(Error.Validation("ReportDispatchLog.EmptyReportId", "O identificador do relatório é obrigatório."));

        if (string.IsNullOrWhiteSpace(recipient))
            return Result<ReportDispatchLog>.Failure(Error.Validation("ReportDispatchLog.EmptyRecipient", "O destinatário é obrigatório."));

        return Result<ReportDispatchLog>.Success(new ReportDispatchLog(
            id,
            generatedReportId,
            channel,
            recipient.Trim(),
            dispatchedAtUtc,
            isSuccess: true,
            errorMessage: null));
    }

    /// <summary>
    /// Cria um registro de auditoria de despacho de relatório com falha.
    /// </summary>
    public static Result<ReportDispatchLog> CreateFailure(
        Guid id,
        Guid generatedReportId,
        ReportDeliveryChannel channel,
        string recipient,
        DateTime dispatchedAtUtc,
        string errorMessage)
    {
        if (id == Guid.Empty)
            return Result<ReportDispatchLog>.Failure(Error.Validation("ReportDispatchLog.EmptyId", "O identificador do log é obrigatório."));

        if (generatedReportId == Guid.Empty)
            return Result<ReportDispatchLog>.Failure(Error.Validation("ReportDispatchLog.EmptyReportId", "O identificador do relatório é obrigatório."));

        if (string.IsNullOrWhiteSpace(recipient))
            return Result<ReportDispatchLog>.Failure(Error.Validation("ReportDispatchLog.EmptyRecipient", "O destinatário é obrigatório."));

        return Result<ReportDispatchLog>.Success(new ReportDispatchLog(
            id,
            generatedReportId,
            channel,
            recipient.Trim(),
            dispatchedAtUtc,
            isSuccess: false,
            errorMessage: string.IsNullOrWhiteSpace(errorMessage) ? "Erro desconhecido no canal de envio." : errorMessage.Trim()));
    }
}

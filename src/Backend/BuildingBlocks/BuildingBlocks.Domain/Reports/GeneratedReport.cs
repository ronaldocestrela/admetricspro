using System.Security.Cryptography;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Reports;

/// <summary>
/// Representa uma instância compilada de relatório executivo white-label com link web interativo e PDF.
/// </summary>
public sealed class GeneratedReport : Entity<Guid>
{
    /// <summary>
    /// Construtor privado para o EF Core.
    /// </summary>
    private GeneratedReport() : base(Guid.Empty)
    {
        Title = string.Empty;
        ShareToken = string.Empty;
        ReportDataPayloadJson = string.Empty;
    }

    /// <summary>
    /// Construtor privado para criação via fábrica de domínio.
    /// </summary>
    private GeneratedReport(
        Guid id,
        Guid workspaceId,
        Guid? reportScheduleId,
        string title,
        DateTime dateRangeStartUtc,
        DateTime dateRangeEndUtc,
        string shareToken,
        DateTime? shareTokenExpiresAtUtc,
        byte[]? pdfContent,
        string? pdfObjectKey,
        string reportDataPayloadJson,
        ReportStatus status,
        DateTime generatedAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        ReportScheduleId = reportScheduleId;
        Title = title;
        DateRangeStartUtc = dateRangeStartUtc;
        DateRangeEndUtc = dateRangeEndUtc;
        ShareToken = shareToken;
        ShareTokenExpiresAtUtc = shareTokenExpiresAtUtc;
        PdfContent = pdfContent;
        PdfObjectKey = pdfObjectKey;
        ReportDataPayloadJson = reportDataPayloadJson;
        Status = status;
        GeneratedAtUtc = generatedAtUtc;
    }

    /// <summary>
    /// Identificador do Workspace avaliado.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Identificador da regra de agendamento que originou a geração (ou nulo se gerado sob demanda).
    /// </summary>
    public Guid? ReportScheduleId { get; private set; }

    /// <summary>
    /// Título do relatório (ex: "Relatório de Performance - Alfa Comércio").
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Data inicial UTC das métricas consolidadas.
    /// </summary>
    public DateTime DateRangeStartUtc { get; private set; }

    /// <summary>
    /// Data final UTC das métricas consolidadas.
    /// </summary>
    public DateTime DateRangeEndUtc { get; private set; }

    /// <summary>
    /// Token seguro e único de alta entropia para acesso web público/interativo.
    /// </summary>
    public string ShareToken { get; private set; }

    /// <summary>
    /// Data limite de expiração do link web interativo (opcional).
    /// </summary>
    public DateTime? ShareTokenExpiresAtUtc { get; private set; }

    /// <summary>
    /// Conteúdo binário legado do PDF (janela de migração; novos relatórios usam <see cref="PdfObjectKey"/>).
    /// </summary>
    public byte[]? PdfContent { get; private set; }

    /// <summary>
    /// Chave do PDF no bucket de relatórios (<c>{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf</c>).
    /// </summary>
    public string? PdfObjectKey { get; private set; }

    /// <summary>
    /// Indica se há PDF disponível por objeto ou coluna legada.
    /// </summary>
    public bool HasPdfAvailable =>
        !string.IsNullOrWhiteSpace(PdfObjectKey)
        || PdfContent is { Length: > 0 };

    /// <summary>
    /// Payload JSON estruturado com todos os KPIs, comparativos de canais, criativos e snapshot de branding.
    /// </summary>
    public string ReportDataPayloadJson { get; private set; }

    /// <summary>
    /// Status atual do relatório no pipeline de geração e entrega.
    /// </summary>
    public ReportStatus Status { get; private set; }

    /// <summary>
    /// Data e hora UTC em que o relatório foi compilado.
    /// </summary>
    public DateTime GeneratedAtUtc { get; private set; }

    /// <summary>
    /// Cria uma nova instância de <see cref="GeneratedReport"/> gerando token seguro de compartilhamento.
    /// </summary>
    public static Result<GeneratedReport> Create(
        Guid id,
        Guid workspaceId,
        Guid? reportScheduleId,
        string title,
        DateTime dateRangeStartUtc,
        DateTime dateRangeEndUtc,
        DateTime? shareTokenExpiresAtUtc,
        byte[]? pdfContent,
        string reportDataPayloadJson,
        DateTime generatedAtUtc,
        string? customToken = null,
        string? pdfObjectKey = null)
    {
        if (id == Guid.Empty)
            return Result<GeneratedReport>.Failure(Error.Validation("GeneratedReport.EmptyId", "O identificador do relatório é obrigatório."));

        if (workspaceId == Guid.Empty)
            return Result<GeneratedReport>.Failure(Error.Validation("GeneratedReport.EmptyWorkspaceId", "O identificador do Workspace é obrigatório."));

        if (string.IsNullOrWhiteSpace(title))
            return Result<GeneratedReport>.Failure(Error.Validation("GeneratedReport.EmptyTitle", "O título do relatório é obrigatório."));

        if (dateRangeEndUtc < dateRangeStartUtc)
            return Result<GeneratedReport>.Failure(Error.Validation("GeneratedReport.InvalidDateRange", "A data final do relatório não pode ser anterior à data inicial."));

        var token = !string.IsNullOrWhiteSpace(customToken)
            ? customToken.Trim()
            : GenerateSecureShareToken();

        var report = new GeneratedReport(
            id,
            workspaceId,
            reportScheduleId,
            title.Trim(),
            dateRangeStartUtc,
            dateRangeEndUtc,
            token,
            shareTokenExpiresAtUtc,
            pdfContent,
            pdfObjectKey,
            reportDataPayloadJson,
            ReportStatus.Generated,
            generatedAtUtc);

        return Result<GeneratedReport>.Success(report);
    }

    /// <summary>
    /// Verifica se o link interativo deste relatório expirou com base no tempo de referência.
    /// </summary>
    public bool IsExpired(DateTime atUtc)
    {
        return ShareTokenExpiresAtUtc.HasValue && atUtc > ShareTokenExpiresAtUtc.Value;
    }

    /// <summary>
    /// Marca o status como despachado aos destinatários.
    /// </summary>
    public void MarkDispatched()
    {
        Status = ReportStatus.Dispatched;
    }

    /// <summary>
    /// Marca o status como falha no processamento ou entrega.
    /// </summary>
    public void MarkFailed()
    {
        Status = ReportStatus.Failed;
    }

    /// <summary>
    /// Atualiza os bytes do PDF na coluna legada (somente migração).
    /// </summary>
    public void AttachPdf(byte[] pdfBytes)
    {
        PdfContent = pdfBytes;
    }

    /// <summary>
    /// Associa a chave do PDF no bucket de relatórios.
    /// </summary>
    /// <param name="objectKey">Chave validada pelo módulo.</param>
    public void AssignPdfObjectKey(string objectKey)
    {
        PdfObjectKey = string.IsNullOrWhiteSpace(objectKey) ? null : objectKey.Trim();
    }

    /// <summary>
    /// Gera um token alfanumérico seguro com entropia criptográfica.
    /// </summary>
    private static string GenerateSecureShareToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

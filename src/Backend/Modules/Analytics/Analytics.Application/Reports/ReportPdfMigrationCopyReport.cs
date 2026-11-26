namespace Analytics.Application.Reports;

/// <summary>
/// Contagens do job one-shot de cópia de <c>PdfContent</c> para object storage.
/// </summary>
/// <param name="Copied">Objetos enviados ou chaves preenchidas nesta execução.</param>
/// <param name="Skipped">Já existentes no bucket com o mesmo tamanho.</param>
/// <param name="Failed">Falhas de put ou persistência da chave.</param>
public sealed record ReportPdfMigrationCopyReport(int Copied, int Skipped, int Failed);
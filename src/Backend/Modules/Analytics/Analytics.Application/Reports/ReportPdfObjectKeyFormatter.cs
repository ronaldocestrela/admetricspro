namespace Analytics.Application.Reports;

/// <summary>
/// Formata chaves de PDF de relatório no bucket <c>admetricspro-reports</c>.
/// </summary>
public static class ReportPdfObjectKeyFormatter
{
    /// <summary>
    /// Monta a chave padrão <c>{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf</c>.
    /// </summary>
    /// <param name="tenantId">Tenant dono do relatório.</param>
    /// <param name="workspaceId">Workspace avaliado.</param>
    /// <param name="reportId">Identificador do relatório gerado.</param>
    /// <returns>Chave relativa no bucket.</returns>
    public static string Build(Guid tenantId, Guid workspaceId, Guid reportId) =>
        $"{tenantId:N}/{workspaceId:N}/{reportId:N}.pdf";
}
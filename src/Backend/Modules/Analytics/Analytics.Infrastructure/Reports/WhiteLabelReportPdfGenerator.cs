using System.Globalization;
using System.Text;
using Analytics.Domain.Reports;
using BuildingBlocks.Domain.Primitives;

namespace Analytics.Infrastructure.Reports;

/// <summary>
/// Gerador de relatórios executivos em formato PDF vetorial com White-Label estrito.
/// Renderiza o documento binário padrão PDF-1.4 sem dependências externas nem menções à plataforma.
/// </summary>
public sealed class WhiteLabelReportPdfGenerator : IReportPdfGenerator
{
    /// <inheritdoc />
    public Task<Result<byte[]>> GeneratePdfAsync(ReportRenderModel model, CancellationToken cancellationToken = default)
    {
        if (model is null)
        {
            return Task.FromResult(Result<byte[]>.Failure(Error.Validation(
                "ReportPdf.NullModel",
                "O modelo de dados do relatório não foi fornecido.")));
        }

        try
        {
            var pdfBytes = BuildPdfDocument(model);
            return Task.FromResult(Result<byte[]>.Success(pdfBytes));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<byte[]>.Failure(Error.Failure(
                "ReportPdf.GenerationFailed",
                $"Falha ao renderizar PDF do relatório: {ex.Message}")));
        }
    }

    private static byte[] BuildPdfDocument(ReportRenderModel model)
    {
        var primaryRgb = HexToRgb(model.Branding?.PrimaryColor ?? "#2563EB");
        var secondaryRgb = HexToRgb(model.Branding?.SecondaryColor ?? "#0F172A");

        var contentStream = new StringBuilder();

        // 1. Top Header Banner (Primary Color)
        // Dimensões da página A4 padrão: 595 x 842 pt
        contentStream.AppendLine($"{primaryRgb.r.ToString("0.00", CultureInfo.InvariantCulture)} {primaryRgb.g.ToString("0.00", CultureInfo.InvariantCulture)} {primaryRgb.b.ToString("0.00", CultureInfo.InvariantCulture)} rg");
        contentStream.AppendLine("0 740 595 102 re f");

        // 2. Header Text (Agency Name and Subtitle)
        var agencyName = SanitizeAscii(string.IsNullOrWhiteSpace(model.Branding?.AgencyName) ? "Agencia de Performance" : model.Branding.AgencyName);
        var reportTitle = SanitizeAscii(model.ReportTitle);
        var workspaceName = SanitizeAscii(model.WorkspaceName);
        var periodText = $"{model.DateRangeStart:dd/MM/yyyy} a {model.DateRangeEnd:dd/MM/yyyy}";

        contentStream.AppendLine("1.0 1.0 1.0 rg"); // Branco
        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F2 18 Tf");
        contentStream.AppendLine("30 805 Td");
        contentStream.AppendLine($"({agencyName}) Tj");
        contentStream.AppendLine("ET");

        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F1 12 Tf");
        contentStream.AppendLine("30 785 Td");
        contentStream.AppendLine($"({reportTitle} - {workspaceName}) Tj");
        contentStream.AppendLine("ET");

        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F1 9 Tf");
        contentStream.AppendLine("30 765 Td");
        contentStream.AppendLine($"(Periodo analisado: {periodText} | Emitido em: {model.GeneratedAt:dd/MM/yyyy HH:mm} UTC) Tj");
        contentStream.AppendLine("ET");

        // 3. KPI Summary Section (Cartões de métricas)
        // Background card
        contentStream.AppendLine("0.95 0.96 0.98 rg"); // Cinza claro
        contentStream.AppendLine("30 650 535 70 re f");

        contentStream.AppendLine($"{secondaryRgb.r.ToString("0.00", CultureInfo.InvariantCulture)} {secondaryRgb.g.ToString("0.00", CultureInfo.InvariantCulture)} {secondaryRgb.b.ToString("0.00", CultureInfo.InvariantCulture)} rg");
        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F2 10 Tf");
        contentStream.AppendLine("45 695 Td");
        contentStream.AppendLine($"(INVESTIMENTO: R$ {model.KpiSummary.TotalSpend:N2}) Tj");
        contentStream.AppendLine("140 0 Td");
        contentStream.AppendLine($"(RECEITA: R$ {model.KpiSummary.TotalRevenue:N2}) Tj");
        contentStream.AppendLine("140 0 Td");
        contentStream.AppendLine($"(ROAS BLENDED: {model.KpiSummary.BlendedRoas:N2}x) Tj");
        contentStream.AppendLine("ET");

        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F1 9 Tf");
        contentStream.AppendLine("45 668 Td");
        contentStream.AppendLine($"(Conversoes: {model.KpiSummary.TotalConversions}) Tj");
        contentStream.AppendLine("140 0 Td");
        contentStream.AppendLine($"(CPA Blended: R$ {model.KpiSummary.BlendedCpa:N2}) Tj");
        contentStream.AppendLine("140 0 Td");
        contentStream.AppendLine($"(Cliques: {model.KpiSummary.TotalClicks:N0} | Impressoes: {model.KpiSummary.TotalImpressions:N0}) Tj");
        contentStream.AppendLine("ET");

        // 4. Platform Breakdown Section
        contentStream.AppendLine("0.2 0.2 0.2 rg");
        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F2 12 Tf");
        contentStream.AppendLine("30 620 Td");
        contentStream.AppendLine("(Desempenho por Plataforma de Midia) Tj");
        contentStream.AppendLine("ET");

        // Tabela de Plataformas
        int currentY = 595;
        contentStream.AppendLine("0.9 0.92 0.95 rg");
        contentStream.AppendLine($"30 {currentY} 535 18 re f");
        contentStream.AppendLine("0.1 0.1 0.1 rg");
        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F2 8 Tf");
        contentStream.AppendLine($"40 {currentY + 5} Td");
        contentStream.AppendLine("(Canal) Tj");
        contentStream.AppendLine("120 0 Td");
        contentStream.AppendLine("(Investimento) Tj");
        contentStream.AppendLine("100 0 Td");
        contentStream.AppendLine("(Receita) Tj");
        contentStream.AppendLine("80 0 Td");
        contentStream.AppendLine("(ROAS) Tj");
        contentStream.AppendLine("80 0 Td");
        contentStream.AppendLine("(Conversoes) Tj");
        contentStream.AppendLine("80 0 Td");
        contentStream.AppendLine("(Share %) Tj");
        contentStream.AppendLine("ET");

        currentY -= 20;
        foreach (var ch in model.ChannelBreakdown)
        {
            contentStream.AppendLine("BT");
            contentStream.AppendLine("/F1 8 Tf");
            contentStream.AppendLine($"40 {currentY} Td");
            contentStream.AppendLine($"({SanitizeAscii(ch.Platform)}) Tj");
            contentStream.AppendLine("120 0 Td");
            contentStream.AppendLine($"(R$ {ch.Spend:N2}) Tj");
            contentStream.AppendLine("100 0 Td");
            contentStream.AppendLine($"(R$ {ch.Revenue:N2}) Tj");
            contentStream.AppendLine("80 0 Td");
            contentStream.AppendLine($"({ch.Roas:N2}x) Tj");
            contentStream.AppendLine("80 0 Td");
            contentStream.AppendLine($"({ch.Conversions}) Tj");
            contentStream.AppendLine("80 0 Td");
            contentStream.AppendLine($"({ch.SharePercentage:N1}%) Tj");
            contentStream.AppendLine("ET");
            currentY -= 16;
        }

        // 5. Top Creatives (se houver)
        if (model.TopCreatives.Count > 0)
        {
            currentY -= 10;
            contentStream.AppendLine("0.2 0.2 0.2 rg");
            contentStream.AppendLine("BT");
            contentStream.AppendLine("/F2 12 Tf");
            contentStream.AppendLine($"30 {currentY} Td");
            contentStream.AppendLine("(Criativos de Maior Destaque) Tj");
            contentStream.AppendLine("ET");

            currentY -= 20;
            foreach (var cr in model.TopCreatives.Take(4))
            {
                var adTitle = SanitizeAscii(cr.AdName.Length > 35 ? cr.AdName[..35] + "..." : cr.AdName);
                contentStream.AppendLine("BT");
                contentStream.AppendLine("/F1 8 Tf");
                contentStream.AppendLine($"40 {currentY} Td");
                contentStream.AppendLine($"({adTitle} [{SanitizeAscii(cr.Platform)}]) Tj");
                contentStream.AppendLine("200 0 Td");
                contentStream.AppendLine($"(Gasto: R$ {cr.Spend:N2} | CTR: {cr.Ctr:N2}% | ROAS: {cr.Roas:N2}x | Status: {SanitizeAscii(cr.FatigueStatus)}) Tj");
                contentStream.AppendLine("ET");
                currentY -= 14;
            }
        }

        // 6. Copilot Insights (se houver)
        if (model.CopilotInsights.Count > 0)
        {
            currentY -= 10;
            contentStream.AppendLine("0.2 0.2 0.2 rg");
            contentStream.AppendLine("BT");
            contentStream.AppendLine("/F2 11 Tf");
            contentStream.AppendLine($"30 {currentY} Td");
            contentStream.AppendLine("(Diagnosticos do Copiloto de Inteligencia Artificial) Tj");
            contentStream.AppendLine("ET");

            currentY -= 16;
            foreach (var insight in model.CopilotInsights.Take(3))
            {
                var text = SanitizeAscii(insight.Length > 95 ? insight[..95] + "..." : insight);
                contentStream.AppendLine("BT");
                contentStream.AppendLine("/F1 8 Tf");
                contentStream.AppendLine($"40 {currentY} Td");
                contentStream.AppendLine($"(* {text}) Tj");
                contentStream.AppendLine("ET");
                currentY -= 13;
            }
        }

        // 7. Manager Notes (se houver)
        if (!string.IsNullOrWhiteSpace(model.CustomNotes))
        {
            currentY -= 10;
            contentStream.AppendLine("0.2 0.2 0.2 rg");
            contentStream.AppendLine("BT");
            contentStream.AppendLine("/F2 10 Tf");
            contentStream.AppendLine($"30 {currentY} Td");
            contentStream.AppendLine("(Observacoes Estrategicas da Gestao) Tj");
            contentStream.AppendLine("ET");

            currentY -= 15;
            var notes = SanitizeAscii(model.CustomNotes.Length > 150 ? model.CustomNotes[..150] + "..." : model.CustomNotes);
            contentStream.AppendLine("BT");
            contentStream.AppendLine("/F1 8 Tf");
            contentStream.AppendLine($"40 {currentY} Td");
            contentStream.AppendLine($"({notes}) Tj");
            contentStream.AppendLine("ET");
        }

        // 8. Footer (Agency White-Label Details) - NEVER mentions AdMetricsPro
        contentStream.AppendLine("0.8 0.8 0.8 rg");
        contentStream.AppendLine("30 45 535 1 re f"); // Linha divisória

        contentStream.AppendLine("0.4 0.4 0.4 rg");
        contentStream.AppendLine("BT");
        contentStream.AppendLine("/F1 8 Tf");
        contentStream.AppendLine("30 30 Td");
        var contactParts = new List<string> { agencyName };
        if (!string.IsNullOrWhiteSpace(model.Branding?.SupportEmail))
            contactParts.Add($"Email: {SanitizeAscii(model.Branding.SupportEmail)}");
        if (!string.IsNullOrWhiteSpace(model.Branding?.SupportPhone))
            contactParts.Add($"Tel: {SanitizeAscii(model.Branding.SupportPhone)}");
        if (!string.IsNullOrWhiteSpace(model.Branding?.CustomDomain))
            contactParts.Add(SanitizeAscii(model.Branding.CustomDomain));

        contentStream.AppendLine($"({string.Join(" | ", contactParts)}) Tj");
        contentStream.AppendLine("ET");

        if (!string.IsNullOrWhiteSpace(model.ShareUrl))
        {
            contentStream.AppendLine("BT");
            contentStream.AppendLine("/F1 7 Tf");
            contentStream.AppendLine("30 18 Td");
            contentStream.AppendLine($"(Acesse a versao web interativa em: {SanitizeAscii(model.ShareUrl)}) Tj");
            contentStream.AppendLine("ET");
        }

        var streamBytes = Encoding.ASCII.GetBytes(contentStream.ToString());

        // Monta os objetos PDF
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");

        var offsets = new List<long>();

        // 1 0 obj: Catalog
        offsets.Add(sb.Length);
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

        // 2 0 obj: Pages
        offsets.Add(sb.Length);
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

        // 3 0 obj: Page
        offsets.Add(sb.Length);
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\nendobj\n");

        // 4 0 obj: Contents stream
        offsets.Add(sb.Length);
        sb.Append($"4 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        sb.Append(contentStream.ToString());
        sb.Append("\nendstream\nendobj\n");

        // 5 0 obj: Font Helvetica
        offsets.Add(sb.Length);
        sb.Append("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        // 6 0 obj: Font Helvetica-Bold
        offsets.Add(sb.Length);
        sb.Append("6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\nendobj\n");

        // Cross-reference table
        var xrefOffset = sb.Length;
        sb.Append($"xref\n0 {offsets.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            sb.Append($"{offset:D10} 00000 n \n");
        }

        // Trailer
        sb.Append($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static (float r, float g, float b) HexToRgb(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return (0.14f, 0.38f, 0.92f); // Default #2563EB

        var clean = hex.Trim().TrimStart('#');
        if (clean.Length == 3)
        {
            clean = $"{clean[0]}{clean[0]}{clean[1]}{clean[1]}{clean[2]}{clean[2]}";
        }

        if (clean.Length == 6 &&
            int.TryParse(clean[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
            int.TryParse(clean[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
            int.TryParse(clean[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return (r / 255f, g / 255f, b / 255f);
        }

        return (0.14f, 0.38f, 0.92f);
    }

    private static string SanitizeAscii(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                // PDF strings escape parenthesis and backslash
                if (c == '(' || c == ')' || c == '\\')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c >= 32 && c <= 126)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(' ');
                }
            }
        }

        return sb.ToString().Trim();
    }
}

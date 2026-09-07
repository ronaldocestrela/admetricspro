namespace Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;

/// <summary>
/// Resposta com o diagnóstico de validação e disponibilidade de um documento fiscal (CPF ou CNPJ).
/// </summary>
/// <param name="Document">Documento sanitizado (apenas dígitos numéricos).</param>
/// <param name="FormattedDocument">Documento formatado com máscara visual oficial.</param>
/// <param name="IsValid">Indica se o documento é matematicamente e formalmente válido.</param>
/// <param name="IsAvailable">Indica se o documento está livre para cadastro de novo inquilino.</param>
/// <param name="DocumentType">Tipo identificado do documento: "CPF", "CNPJ" ou "Unknown".</param>
/// <param name="Reason">Descrição em caso de documento inválido ou já cadastrado.</param>
public sealed record TaxDocumentAvailabilityResponse(
    string Document,
    string FormattedDocument,
    bool IsValid,
    bool IsAvailable,
    string DocumentType,
    string? Reason = null);

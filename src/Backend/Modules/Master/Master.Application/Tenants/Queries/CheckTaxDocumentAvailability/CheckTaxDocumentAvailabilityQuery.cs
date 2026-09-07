using BuildingBlocks.Application.Messaging;

namespace Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;

/// <summary>
/// Consulta para validar formato, dígitos verificadores e disponibilidade de documento fiscal (CPF ou CNPJ) no catálogo Master.
/// </summary>
/// <param name="Document">Número do documento fiscal a ser analisado (com ou sem pontuação).</param>
public sealed record CheckTaxDocumentAvailabilityQuery(string Document) : IQuery<TaxDocumentAvailabilityResponse>;

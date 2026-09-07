using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Master.Application.Repositories;

namespace Master.Application.Tenants.Queries.CheckTaxDocumentAvailability;

/// <summary>
/// Manipulador da consulta <see cref="CheckTaxDocumentAvailabilityQuery"/>.
/// Executa a validação matemática oficial do documento (CPF ou CNPJ) e consulta o catálogo Master quanto à duplicidade.
/// </summary>
public sealed class CheckTaxDocumentAvailabilityQueryHandler : IQueryHandler<CheckTaxDocumentAvailabilityQuery, TaxDocumentAvailabilityResponse>
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// Inicializa uma nova instância de <see cref="CheckTaxDocumentAvailabilityQueryHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repositório de persistência de tenants.</param>
    public CheckTaxDocumentAvailabilityQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <inheritdoc />
    public async Task<Result<TaxDocumentAvailabilityResponse>> Handle(CheckTaxDocumentAvailabilityQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sanitized = TaxDocumentValidator.Sanitize(query.Document);
        var formatted = TaxDocumentValidator.Format(sanitized);

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return Result<TaxDocumentAvailabilityResponse>.Success(
                new TaxDocumentAvailabilityResponse(
                    Document: string.Empty,
                    FormattedDocument: string.Empty,
                    IsValid: false,
                    IsAvailable: false,
                    DocumentType: "Unknown",
                    Reason: "Documento fiscal não pode ser vazio."));
        }

        var isLengthValid = TaxDocumentValidator.IsValidFormat(sanitized);
        if (!isLengthValid)
        {
            return Result<TaxDocumentAvailabilityResponse>.Success(
                new TaxDocumentAvailabilityResponse(
                    Document: sanitized,
                    FormattedDocument: formatted,
                    IsValid: false,
                    IsAvailable: false,
                    DocumentType: "Unknown",
                    Reason: "O documento deve conter 11 dígitos (CPF) ou 14 dígitos (CNPJ)."));
        }

        var documentType = sanitized.Length == 11 ? "CPF" : "CNPJ";
        var isChecksumValid = TaxDocumentValidator.IsValid(sanitized);

        if (!isChecksumValid)
        {
            return Result<TaxDocumentAvailabilityResponse>.Success(
                new TaxDocumentAvailabilityResponse(
                    Document: sanitized,
                    FormattedDocument: formatted,
                    IsValid: false,
                    IsAvailable: false,
                    DocumentType: documentType,
                    Reason: $"O {documentType} informado possui dígitos verificadores inválidos."));
        }

        // Verifica unicidade no catálogo Master
        var existingTenant = await _tenantRepository.GetByCnpjAsync(sanitized, cancellationToken);
        if (existingTenant is not null)
        {
            return Result<TaxDocumentAvailabilityResponse>.Success(
                new TaxDocumentAvailabilityResponse(
                    Document: sanitized,
                    FormattedDocument: formatted,
                    IsValid: true,
                    IsAvailable: false,
                    DocumentType: documentType,
                    Reason: $"O {documentType} informado já está em uso por outro inquilino cadastrado."));
        }

        return Result<TaxDocumentAvailabilityResponse>.Success(
            new TaxDocumentAvailabilityResponse(
                Document: sanitized,
                FormattedDocument: formatted,
                IsValid: true,
                IsAvailable: true,
                DocumentType: documentType));
    }
}

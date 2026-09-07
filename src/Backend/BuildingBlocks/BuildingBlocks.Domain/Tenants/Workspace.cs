using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Agregado de domínio que representa um cliente gerenciado (Workspace) da agência no contexto do banco isolado do inquilino.
/// </summary>
public sealed class Workspace : AggregateRoot<Guid>
{
    private Workspace(
        Guid id,
        string name,
        string cnpjOrCpf,
        decimal monthlyAdSpendBudget,
        string? segment,
        bool isActive,
        DateTime createdAtUtc,
        DateTime? updatedAtUtc)
        : base(id)
    {
        Name = name;
        CnpjOrCpf = cnpjOrCpf;
        MonthlyAdSpendBudget = monthlyAdSpendBudget;
        Segment = segment;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    private Workspace()
        : base(Guid.Empty)
    {
        Name = string.Empty;
        CnpjOrCpf = string.Empty;
        MonthlyAdSpendBudget = 0m;
        Segment = null;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = null;
    }

    /// <summary>
    /// Obtém o nome comercial ou razão social do cliente/workspace.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Obtém o documento fiscal (CPF com 11 dígitos ou CNPJ com 14 dígitos) sanitizado.
    /// </summary>
    public string CnpjOrCpf { get; private set; }

    /// <summary>
    /// Obtém o orçamento mensal previsto de investimento em anúncios (Ad Spend) para o cliente.
    /// </summary>
    public decimal MonthlyAdSpendBudget { get; private set; }

    /// <summary>
    /// Obtém o nicho ou segmento de atuação mercadológica do cliente (ex: E-commerce, Infoproduto, Local).
    /// </summary>
    public string? Segment { get; private set; }

    /// <summary>
    /// Obtém o status operacional do workspace (ativo ou pausado/inativo).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Obtém a data e hora em UTC de criação do registro.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a data e hora em UTC da última modificação cadastral, se houver.
    /// </summary>
    public DateTime? UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Método de fábrica para criar uma nova instância de <see cref="Workspace"/> validando invariantes de negócio.
    /// </summary>
    /// <param name="id">Identificador único do workspace.</param>
    /// <param name="name">Nome do cliente/workspace.</param>
    /// <param name="cnpjOrCpf">Documento fiscal bruto ou formatado.</param>
    /// <param name="monthlyAdSpendBudget">Orçamento mensal de tráfego pago.</param>
    /// <param name="segment">Segmento opcional de mercado.</param>
    /// <returns>Resultado contendo a entidade criada ou erro semântico de validação.</returns>
    public static Result<Workspace> Create(
        Guid id,
        string name,
        string cnpjOrCpf,
        decimal monthlyAdSpendBudget,
        string? segment)
    {
        if (id == Guid.Empty)
        {
            id = Guid.NewGuid();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Workspace>.Failure(Error.Validation("Workspace.NameRequired", "O nome do workspace é obrigatório."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 150)
        {
            return Result<Workspace>.Failure(Error.Validation("Workspace.NameTooLong", "O nome do workspace não pode exceder 150 caracteres."));
        }

        var sanitizedDocument = TaxDocumentValidator.Sanitize(cnpjOrCpf);
        if (!TaxDocumentValidator.IsValidFormat(sanitizedDocument) ||
            !(TaxDocumentValidator.IsValidCpf(sanitizedDocument) || TaxDocumentValidator.IsValidCnpj(sanitizedDocument)))
        {
            return Result<Workspace>.Failure(Error.Validation("Workspace.InvalidTaxDocument", "O documento informado não é um CPF ou CNPJ válido."));
        }

        if (monthlyAdSpendBudget < 0)
        {
            return Result<Workspace>.Failure(Error.Validation("Workspace.InvalidBudget", "O orçamento mensal de mídia não pode ser negativo."));
        }

        var trimmedSegment = string.IsNullOrWhiteSpace(segment) ? null : segment.Trim();
        if (trimmedSegment is not null && trimmedSegment.Length > 100)
        {
            return Result<Workspace>.Failure(Error.Validation("Workspace.SegmentTooLong", "O segmento não pode exceder 100 caracteres."));
        }

        var workspace = new Workspace(
            id,
            trimmedName,
            sanitizedDocument,
            monthlyAdSpendBudget,
            trimmedSegment,
            isActive: true,
            createdAtUtc: DateTime.UtcNow,
            updatedAtUtc: null);

        return Result<Workspace>.Success(workspace);
    }

    /// <summary>
    /// Atualiza os dados operacionais e mercadológicos do cliente/workspace.
    /// </summary>
    /// <param name="name">Novo nome comercial.</param>
    /// <param name="cnpjOrCpf">Novo documento fiscal.</param>
    /// <param name="monthlyAdSpendBudget">Novo orçamento de investimento mensal.</param>
    /// <param name="segment">Novo segmento de atuação.</param>
    /// <returns>Resultado da operação.</returns>
    public Result UpdateDetails(
        string name,
        string cnpjOrCpf,
        decimal monthlyAdSpendBudget,
        string? segment)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Workspace.NameRequired", "O nome do workspace é obrigatório."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 150)
        {
            return Result.Failure(Error.Validation("Workspace.NameTooLong", "O nome do workspace não pode exceder 150 caracteres."));
        }

        var sanitizedDocument = TaxDocumentValidator.Sanitize(cnpjOrCpf);
        if (!TaxDocumentValidator.IsValidFormat(sanitizedDocument) ||
            !(TaxDocumentValidator.IsValidCpf(sanitizedDocument) || TaxDocumentValidator.IsValidCnpj(sanitizedDocument)))
        {
            return Result.Failure(Error.Validation("Workspace.InvalidTaxDocument", "O documento informado não é um CPF ou CNPJ válido."));
        }

        if (monthlyAdSpendBudget < 0)
        {
            return Result.Failure(Error.Validation("Workspace.InvalidBudget", "O orçamento mensal de mídia não pode ser negativo."));
        }

        var trimmedSegment = string.IsNullOrWhiteSpace(segment) ? null : segment.Trim();
        if (trimmedSegment is not null && trimmedSegment.Length > 100)
        {
            return Result.Failure(Error.Validation("Workspace.SegmentTooLong", "O segmento não pode exceder 100 caracteres."));
        }

        Name = trimmedName;
        CnpjOrCpf = sanitizedDocument;
        MonthlyAdSpendBudget = monthlyAdSpendBudget;
        Segment = trimmedSegment;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Desativa o workspace, impedindo disparos de automações e coletas ativas.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("Workspace.AlreadyInactive", "O workspace já se encontra inativo."));
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }

    /// <summary>
    /// Reativa o workspace para retomar as rotinas de gestão e campanhas.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Activate()
    {
        if (IsActive)
        {
            return Result.Failure(Error.Conflict("Workspace.AlreadyActive", "O workspace já se encontra ativo."));
        }

        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;

        return Result.Success();
    }
}

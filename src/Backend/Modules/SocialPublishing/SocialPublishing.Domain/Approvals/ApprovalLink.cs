using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace SocialPublishing.Domain.Approvals;

/// <summary>
/// Link compartilhável de aprovação orgânica por workspace (token persistido apenas como hash SHA-256).
/// </summary>
public sealed class ApprovalLink : AggregateRoot<Guid>
{
    private ApprovalLink(
        Guid id,
        Guid workspaceId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    private ApprovalLink()
        : base(Guid.Empty)
    {
        TokenHash = string.Empty;
    }

    /// <summary>
    /// Obtém o workspace ao qual o pacote de aprovação pertence.
    /// </summary>
    public Guid WorkspaceId { get; private set; }

    /// <summary>
    /// Obtém o hash SHA-256 hexadecimal (64 caracteres) do token opaco.
    /// </summary>
    public string TokenHash { get; private set; }

    /// <summary>
    /// Obtém o instante UTC em que o link deixa de ser válido.
    /// </summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Obtém o instante UTC de revogação, se o link foi substituído por outro do mesmo workspace.
    /// </summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o carimbo UTC de criação.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Cria um novo link de aprovação com hash já calculado.
    /// </summary>
    /// <param name="workspaceId">Workspace proprietário do pacote.</param>
    /// <param name="tokenHash">Hash SHA-256 hexadecimal do token em claro.</param>
    /// <param name="expiresAtUtc">Expiração obrigatória em UTC.</param>
    /// <param name="createdAtUtc">Instante de criação em UTC.</param>
    /// <returns>Resultado com o agregado ou erro de validação.</returns>
    public static Result<ApprovalLink> Create(
        Guid workspaceId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result<ApprovalLink>.Failure(SocialErrors.WorkspaceRequired);
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != 64)
        {
            return Result<ApprovalLink>.Failure(SocialErrors.ApprovalTokenInvalid);
        }

        if (expiresAtUtc <= createdAtUtc)
        {
            return Result<ApprovalLink>.Failure(SocialErrors.ApprovalLinkExpirationInvalid);
        }

        var link = new ApprovalLink(
            Guid.NewGuid(),
            workspaceId,
            tokenHash.Trim(),
            DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc));

        return Result<ApprovalLink>.Success(link);
    }

    /// <summary>
    /// Indica se o link está ativo no instante informado (não revogado e não expirado).
    /// </summary>
    /// <param name="utcNow">Relógio de referência em UTC.</param>
    /// <returns><c>true</c> quando o link pode ser usado.</returns>
    public bool IsActiveAt(DateTime utcNow) =>
        RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    /// <summary>
    /// Revoga o link substituindo-o por uma nova emissão do mesmo workspace.
    /// </summary>
    /// <param name="revokedAtUtc">Instante UTC da revogação.</param>
    public void Revoke(DateTime revokedAtUtc)
    {
        RevokedAtUtc = DateTime.SpecifyKind(revokedAtUtc, DateTimeKind.Utc);
    }
}

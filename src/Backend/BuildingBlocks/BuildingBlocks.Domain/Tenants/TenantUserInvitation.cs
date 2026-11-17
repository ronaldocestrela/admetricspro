using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Primitives;

namespace BuildingBlocks.Domain.Tenants;

/// <summary>
/// Convite de aceitação de conta para colaboradores do inquilino, com token de uso único e prazo de validade.
/// </summary>
public sealed class TenantUserInvitation : Entity<Guid>
{
    private TenantUserInvitation(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        Guid? createdByUserId,
        DateTime? acceptedAtUtc,
        DateTime? revokedAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
        AcceptedAtUtc = acceptedAtUtc;
        RevokedAtUtc = revokedAtUtc;
    }

    private TenantUserInvitation()
        : base(Guid.Empty)
    {
        TokenHash = string.Empty;
        ExpiresAtUtc = DateTime.UtcNow;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Obtém o identificador do colaborador convidado.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Obtém o hash SHA-256 do token opaco enviado por e-mail.
    /// </summary>
    public string TokenHash { get; private set; }

    /// <summary>
    /// Obtém a data limite de validade do convite em UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Obtém a data de criação do convite em UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Obtém o identificador do usuário que emitiu o convite, se disponível.
    /// </summary>
    public Guid? CreatedByUserId { get; private set; }

    /// <summary>
    /// Obtém a data de aceite do convite em UTC, quando concluído.
    /// </summary>
    public DateTime? AcceptedAtUtc { get; private set; }

    /// <summary>
    /// Obtém a data de revogação do convite em UTC, quando substituído ou cancelado.
    /// </summary>
    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>
    /// Indica se o convite ainda pode ser utilizado.
    /// </summary>
    public bool IsOpen =>
        AcceptedAtUtc is null &&
        RevokedAtUtc is null &&
        ExpiresAtUtc > DateTime.UtcNow;

    /// <summary>
    /// Cria um novo convite pendente para o colaborador informado.
    /// </summary>
    /// <param name="id">Identificador único do convite.</param>
    /// <param name="userId">Identificador do colaborador convidado.</param>
    /// <param name="tokenHash">Hash SHA-256 do token opaco.</param>
    /// <param name="expiresAtUtc">Data de expiração do convite.</param>
    /// <param name="createdByUserId">Identificador opcional de quem emitiu o convite.</param>
    /// <param name="createdAtUtc">Data de criação opcional; padrão é UTC atual.</param>
    /// <returns>Resultado com a entidade criada ou falha de validação.</returns>
    public static Result<TenantUserInvitation> Create(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        Guid? createdByUserId = null,
        DateTime? createdAtUtc = null)
    {
        if (id == Guid.Empty)
        {
            return Result<TenantUserInvitation>.Failure(
                Error.Validation("TenantUserInvitation.InvalidId", "O identificador do convite não pode ser vazio."));
        }

        if (userId == Guid.Empty)
        {
            return Result<TenantUserInvitation>.Failure(
                Error.Validation("TenantUserInvitation.InvalidUserId", "O identificador do colaborador é obrigatório."));
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > 128)
        {
            return Result<TenantUserInvitation>.Failure(
                Error.Validation("TenantUserInvitation.InvalidTokenHash", "O hash do token do convite é inválido."));
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            return Result<TenantUserInvitation>.Failure(
                Error.Validation("TenantUserInvitation.InvalidExpiration", "A expiração do convite deve estar no futuro."));
        }

        var invitation = new TenantUserInvitation(
            id,
            userId,
            tokenHash.Trim(),
            expiresAtUtc,
            createdAtUtc ?? DateTime.UtcNow,
            createdByUserId,
            acceptedAtUtc: null,
            revokedAtUtc: null);

        return Result<TenantUserInvitation>.Success(invitation);
    }

    /// <summary>
    /// Revoga o convite, impedindo futura aceitação.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Revoke()
    {
        if (AcceptedAtUtc is not null)
        {
            return Result.Failure(
                Error.Validation("TenantUserInvitation.AlreadyAccepted", "Convites já aceitos não podem ser revogados."));
        }

        if (RevokedAtUtc is not null)
        {
            return Result.Success();
        }

        RevokedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Marca o convite como aceito pelo colaborador.
    /// </summary>
    /// <returns>Resultado da operação.</returns>
    public Result Accept()
    {
        if (RevokedAtUtc is not null)
        {
            return Result.Failure(
                Error.Validation("TenantUserInvitation.Revoked", "Este convite foi revogado e não pode ser aceito."));
        }

        if (AcceptedAtUtc is not null)
        {
            return Result.Failure(
                Error.Validation("TenantUserInvitation.AlreadyAccepted", "Este convite já foi aceito."));
        }

        if (ExpiresAtUtc <= DateTime.UtcNow)
        {
            return Result.Failure(
                Error.Validation("TenantUserInvitation.Expired", "Este convite expirou."));
        }

        AcceptedAtUtc = DateTime.UtcNow;
        return Result.Success();
    }
}
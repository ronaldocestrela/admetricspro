using BuildingBlocks.Domain.Tenants;

namespace BuildingBlocks.Application.Security;

/// <summary>
/// Provedor contextual para identificação do usuário autenticado no escopo da requisição atual.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>
    /// Obtém o identificador do usuário autenticado (GUID).
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Obtém o endereço de e-mail do usuário autenticado.
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Obtém o papel funcional atribuído ao usuário autenticado, se aplicável.
    /// </summary>
    TenantRole? Role { get; }

    /// <summary>
    /// Obtém um valor indicando se existe um usuário autenticado no contexto atual.
    /// </summary>
    bool IsAuthenticated { get; }
}

namespace WebApi.Models;

/// <summary>
/// Modelo de requisição HTTP para autenticação de usuários operacionais de agência (inquilinos).
/// </summary>
/// <param name="Email">Endereço de e-mail corporativo cadastrado.</param>
/// <param name="Password">Senha em texto plano informada pelo usuário.</param>
/// <param name="Subdomain">Subdomínio opcional caso a requisição não seja originada diretamente do subdomínio da agência.</param>
public sealed record TenantLoginApiRequest(
    string Email,
    string Password,
    string? Subdomain = null);

namespace WebApi.Models;

/// <summary>
/// Modelo de entrada para autenticação de operadores do Backoffice via API.
/// </summary>
/// <param name="Email">E-mail corporativo cadastrado.</param>
/// <param name="Password">Senha do operador.</param>
public sealed record BackofficeLoginApiRequest(string Email, string Password);

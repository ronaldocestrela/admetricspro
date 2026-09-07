using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Master.Application.Users.DTOs;
using Master.Application.Users.Services;

namespace BackofficeApp.Services;

/// <summary>
/// Implementação cliente de <see cref="IBackofficeAuthService"/> que delega a autenticação para a Web API.
/// </summary>
public sealed class BackofficeAuthClientService : IBackofficeAuthService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="BackofficeAuthClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP configurado para acesso à Web API.</param>
    public BackofficeAuthClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedBackofficeUserDto>> AuthenticateAsync(
        string email,
        string password,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync(
                "/api/v1/admin/auth/login",
                payload,
                JsonOptions,
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<AuthenticatedBackofficeUserDto>>(JsonOptions, cancellationToken);
            return result ?? Result<AuthenticatedBackofficeUserDto>.Failure(Error.Failure("Auth.InvalidResponse", "Resposta inválida do servidor de autenticação."));
        }
        catch (Exception ex)
        {
            return Result<AuthenticatedBackofficeUserDto>.Failure(Error.Failure("Auth.NetworkError", ex.Message));
        }
    }
}

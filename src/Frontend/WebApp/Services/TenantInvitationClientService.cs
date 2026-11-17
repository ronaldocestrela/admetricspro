using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Tenants.Application.Users.DTOs;

namespace WebApp.Services;

/// <summary>
/// Implementação de <see cref="ITenantInvitationClientService"/>.
/// </summary>
public sealed class TenantInvitationClientService : ITenantInvitationClientService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantInvitationClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP para a Web API.</param>
    public TenantInvitationClientService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<Result<TenantInvitationPreviewDto>> GetInvitationAsync(
        string token,
        string subdomain,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedToken = Uri.EscapeDataString(token);
            var encodedSubdomain = Uri.EscapeDataString(subdomain);
            var response = await _httpClient.GetAsync(
                $"/api/v1/public/tenant-invitations/{encodedToken}?subdomain={encodedSubdomain}",
                cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<Result<TenantInvitationPreviewDto>>(JsonOptions, cancellationToken);
            return result ?? Result<TenantInvitationPreviewDto>.Failure(
                Error.Failure("TenantInvitation.InvalidResponse", "Resposta inválida ao consultar convite."));
        }
        catch (Exception ex)
        {
            return Result<TenantInvitationPreviewDto>.Failure(
                Error.Failure("TenantInvitation.NetworkError", $"Erro de comunicação: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> AcceptInvitationAsync(
        string token,
        string subdomain,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                Token = token,
                Subdomain = subdomain,
                Password = password,
                ConfirmPassword = confirmPassword
            };

            var response = await _httpClient.PostAsJsonAsync("/api/v1/public/tenant-invitations/accept", payload, JsonOptions, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);
            return result ?? Result.Failure(
                Error.Failure("TenantInvitation.InvalidResponse", "Resposta inválida ao aceitar convite."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("TenantInvitation.NetworkError", $"Erro de comunicação: {ex.Message}"));
        }
    }
}
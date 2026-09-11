using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Tenants;
using Tenants.Application.Audit.DTOs;
using Tenants.Application.Rbac.DTOs;
using WebApp.State;

namespace WebApp.Services;

/// <summary>
/// Implementação concreta do serviço de cliente HTTP para governança RBAC e auditoria do inquilino.
/// </summary>
public sealed class TenantRbacClientService : ITenantRbacClientService
{
    private readonly HttpClient _httpClient;
    private readonly ITenantStateProvider _tenantStateProvider;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Inicializa uma nova instância de <see cref="TenantRbacClientService"/>.
    /// </summary>
    /// <param name="httpClient">Cliente HTTP injetado apontando para a Web API.</param>
    /// <param name="tenantStateProvider">Provedor de estado do inquilino ativo.</param>
    public TenantRbacClientService(HttpClient httpClient, ITenantStateProvider tenantStateProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _tenantStateProvider = tenantStateProvider ?? throw new ArgumentNullException(nameof(tenantStateProvider));
    }

    /// <inheritdoc />
    public async Task<Result<TenantRbacMatrixDto>> GetRbacMatrixAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "/api/v1/tenants/rbac/matrix");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantRbacMatrixDto>>(JsonOptions, cancellationToken);

            return result ?? Result<TenantRbacMatrixDto>.Failure(
                Error.Failure("Rbac.InvalidResponse", "Resposta inválida ao obter matriz RBAC."));
        }
        catch (Exception ex)
        {
            return Result<TenantRbacMatrixDto>.Failure(
                Error.Failure("Rbac.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TenantUserPermissionsDto>> GetUserPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result<TenantUserPermissionsDto>.Failure(
                Error.Validation("TenantUser.InvalidId", "Identificador de colaborador inválido."));
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tenants/rbac/users/{userId}/permissions");
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantUserPermissionsDto>>(JsonOptions, cancellationToken);

            return result ?? Result<TenantUserPermissionsDto>.Failure(
                Error.Failure("Rbac.InvalidResponse", "Resposta inválida ao obter permissões do colaborador."));
        }
        catch (Exception ex)
        {
            return Result<TenantUserPermissionsDto>.Failure(
                Error.Failure("Rbac.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> ChangeUserRoleAsync(
        Guid userId,
        TenantRole newRole,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure(
                Error.Validation("TenantUser.InvalidId", "Identificador de colaborador inválido."));
        }

        try
        {
            var payload = new { NewRole = newRole };
            using var message = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/tenants/users/{userId}/role")
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions, cancellationToken);

            return result ?? Result.Failure(
                Error.Failure("Rbac.InvalidResponse", "Resposta inválida ao alterar papel de colaborador."));
        }
        catch (Exception ex)
        {
            return Result.Failure(
                Error.Failure("Rbac.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<TenantAuditLogsResponse>> GetAuditLogsAsync(
        Guid? userId = null,
        string? action = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                queryParams.Add($"userId={userId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                queryParams.Add($"action={Uri.EscapeDataString(action.Trim())}");
            }

            if (fromUtc.HasValue)
            {
                queryParams.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("o"))}");
            }

            if (toUtc.HasValue)
            {
                queryParams.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("o"))}");
            }

            var url = $"/api/v1/tenants/audit-logs?{string.Join("&", queryParams)}";
            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            AppendTenantHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<TenantAuditLogsResponse>>(JsonOptions, cancellationToken);

            return result ?? Result<TenantAuditLogsResponse>.Failure(
                Error.Failure("Audit.InvalidResponse", "Resposta inválida ao consultar logs de auditoria."));
        }
        catch (Exception ex)
        {
            return Result<TenantAuditLogsResponse>.Failure(
                Error.Failure("Audit.NetworkError", $"Erro de comunicação com a API: {ex.Message}"));
        }
    }

    private void AppendTenantHeader(HttpRequestMessage request)
    {
        var tenantId = _tenantStateProvider.CurrentTenant?.TenantId;
        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            request.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.Value.ToString());
        }
    }
}

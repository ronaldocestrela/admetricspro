using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Messaging;
using BuildingBlocks.Domain.Primitives;
using WebApp.State;

namespace WebApp.Services.Jobs;

/// <summary>
/// Implementação de <see cref="IUserInitiatedJobClient"/>.
/// </summary>
public sealed class UserInitiatedJobClient(
    HttpClient httpClient,
    ITenantStateProvider tenantStateProvider,
    ITenantSessionStateProvider? session = null) : IUserInitiatedJobClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> GetJobAsync(
        Guid workspaceId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || jobId == Guid.Empty)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Validation("UserJob.InvalidId", "Identificadores inválidos."));
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v1/workspaces/{workspaceId}/jobs/{jobId}");
            AppendTenantHeader(request);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<UserInitiatedJobDto>>(JsonOptions, cancellationToken);

            return result ?? Result<UserInitiatedJobDto>.Failure(
                Error.Failure("UserJob.InvalidResponse", "Resposta inválida ao consultar job."));
        }
        catch (Exception ex)
        {
            return Result<UserInitiatedJobDto>.Failure(
                Error.Failure("UserJob.NetworkError", $"Erro ao consultar job: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserInitiatedJobDto>> WaitForTerminalAsync(
        Guid workspaceId,
        Guid jobId,
        TimeSpan pollInterval,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await GetJobAsync(workspaceId, jobId, cancellationToken);
            if (current.IsFailure)
            {
                return current;
            }

            if (current.Value.Status is UserInitiatedJobStatus.Succeeded or UserInitiatedJobStatus.Failed)
            {
                return current;
            }

            await Task.Delay(pollInterval, cancellationToken);
        }

        return Result<UserInitiatedJobDto>.Failure(
            Error.Failure("UserJob.Timeout", "Tempo esgotado aguardando conclusão do job."));
    }

    private void AppendTenantHeader(HttpRequestMessage message)
    {
        var accessToken = session?.CurrentSession?.AccessToken;
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        var tenantId = tenantStateProvider.CurrentTenant?.TenantId ?? Guid.Empty;
        if (tenantId != Guid.Empty)
        {
            message.Headers.TryAddWithoutValidation("X-Tenant-Id", tenantId.ToString());
        }
    }
}
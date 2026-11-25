using System.Net.Http.Json;
using System.Text.Json;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Domain.Primitives;

namespace BackofficeApp.Services;

/// <summary>
/// Implementação HTTP do cliente de dead-letters.
/// </summary>
public sealed class MessagingDeadLetterClientService(HttpClient httpClient) : IMessagingDeadLetterClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>> GetDeadLettersAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/v1/admin/messaging/dead-letters?limit={limit}",
                cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>>(
                JsonOptions,
                cancellationToken);
            return result ?? Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>.Failure(
                Error.Failure("Messaging.FetchFailed", "Falha ao obter dead-letters."));
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<IntegrationDeadLetterListItemDto>>.Failure(
                Error.Failure("Messaging.NetworkError", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> ReprocessAsync(
        Guid eventId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new { tenantId };
            var response = await httpClient.PostAsJsonAsync(
                $"/api/v1/admin/messaging/dead-letters/{eventId}/reprocess",
                body,
                JsonOptions,
                cancellationToken);
            var result = await response.Content.ReadFromJsonAsync<Result<Guid>>(JsonOptions, cancellationToken);
            return result ?? Result<Guid>.Failure(Error.Failure("Messaging.ReprocessFailed", "Falha ao reprocessar."));
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(Error.Failure("Messaging.NetworkError", ex.Message));
        }
    }
}
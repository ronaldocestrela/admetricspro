using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using Integrations.Application.Options;
using Integrations.Domain.Meta;
using Microsoft.Extensions.Options;

namespace Integrations.Infrastructure.Meta;

/// <summary>Upload multipart autenticado de imagens e vídeos, sem retries de POST.</summary>
/// <param name="client">Cliente HTTP configurado.</param>
/// <param name="options">Configurações validadas da operação.</param>
public sealed class MetaMediaApi(HttpClient client, IOptions<MetaAdsOAuthOptions> options) : IMetaMediaApi
{
    /// <inheritdoc />
    public async Task<Result<MetaMediaResult>> UploadAsync(string accountId, string token, string contentType, string fileName, byte[] content, CancellationToken cancellationToken)
    {
        var image = contentType is "image/png" or "image/jpeg";
        var validBytes = contentType switch
        {
            "image/png" => content.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            "image/jpeg" => content.AsSpan().StartsWith(new byte[] { 255, 216, 255 }),
            "video/mp4" => content.Length > 12 && content.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            _ => false
        };
        if (!validBytes || content.Length > (image ? 10_000_000 : 100_000_000) || fileName.Length is < 1 or > 200 || fileName.Any(char.IsControl) ||
            !accountId.StartsWith("act_", StringComparison.Ordinal) || accountId.Length < 5 || !accountId[4..].All(char.IsAsciiDigit))
            return Result<MetaMediaResult>.Failure(Error.Validation("Meta.InvalidMedia", "Envie PNG/JPEG de até 10 MB ou MP4 de até 100 MB, com conteúdo e nome válidos."));
        var proof = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Value.AppSecret), Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        using var multipart = new MultipartFormDataContent();
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new(contentType);
        multipart.Add(file, image ? "filename" : "source", Path.GetFileName(fileName));
        multipart.Add(new StringContent(proof), "appsecret_proof");
        try
        {
            var host = image ? "graph.facebook.com" : "graph-video.facebook.com";
            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://{host}/{options.Value.GraphApiVersion}/{accountId}/{(image ? "adimages" : "advideos")}") { Content = multipart };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<MetaMediaResult>.Failure((int)response.StatusCode >= 500
                    ? Error.Failure("Meta.WriteOutcomeUnknown", "O resultado do upload é incerto; consulte a conta antes de repetir.")
                    : Error.Validation("Meta.MediaRejected", "A Meta rejeitou o arquivo ou a permissão de upload."));
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            string? id = null;
            if (image && json.RootElement.TryGetProperty("images", out var images) && images.ValueKind == JsonValueKind.Object)
                id = images.EnumerateObject().FirstOrDefault().Value.TryGetProperty("hash", out var hash) ? hash.GetString() : null;
            else if (!image && json.RootElement.TryGetProperty("id", out var videoId)) id = videoId.GetString();
            return string.IsNullOrWhiteSpace(id)
                ? Result<MetaMediaResult>.Failure(Error.Failure("Meta.WriteOutcomeUnknown", "A Meta não confirmou o identificador da mídia enviada."))
                : Result<MetaMediaResult>.Success(new(id, image ? "image" : "video", image ? "Ready" : "Processing"));
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        { return Result<MetaMediaResult>.Failure(Error.Failure("Meta.WriteOutcomeUnknown", "O resultado do upload é incerto; não repetir automaticamente.")); }
    }

    /// <inheritdoc />
    public async Task<Result<MetaMediaResult>> GetVideoStatusAsync(string videoId, string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(videoId) || !videoId.All(char.IsAsciiDigit))
            return Result<MetaMediaResult>.Failure(Error.Validation("Meta.InvalidVideo", "Vídeo inválido."));
        var response = await new MetaGraphClient(client, options.Value).GetAsync($"{videoId}?fields=id,status", token, cancellationToken);
        if (response.IsFailure) return Result<MetaMediaResult>.Failure(response.Error);
        var state = response.Value.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Object && status.TryGetProperty("video_status", out var current) ? current.GetString() : null;
        return Result<MetaMediaResult>.Success(new(videoId, "video", state ?? "unknown"));
    }
}

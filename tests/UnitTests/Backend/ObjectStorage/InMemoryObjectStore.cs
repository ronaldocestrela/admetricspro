using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Implementação em memória de <see cref="IObjectStore"/> apenas para testes unitários (Fase 2).
/// </summary>
public sealed class InMemoryObjectStore : IObjectStore
{
    private readonly Dictionary<(string Bucket, string Key), StoredObject> _objects = new();

    /// <inheritdoc />
    public async Task<Result> PutAsync(
        string bucket,
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var buffer = new MemoryStream();
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await content.CopyToAsync(buffer, cancellationToken);
        _objects[(bucket, key)] = new StoredObject(buffer.ToArray(), contentType);
        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Result<Stream>> OpenReadAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_objects.TryGetValue((bucket, key), out var stored))
        {
            return Task.FromResult(Result<Stream>.Failure(StorageErrors.ObjectNotFound));
        }

        Stream stream = new MemoryStream(stored.Content, writable: false);
        return Task.FromResult(Result<Stream>.Success(stream));
    }

    /// <inheritdoc />
    public Task<Result> DeleteAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _objects.Remove((bucket, key));
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<bool>> ExistsAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var exists = _objects.ContainsKey((bucket, key));
        return Task.FromResult(Result<bool>.Success(exists));
    }

    private sealed record StoredObject(byte[] Content, string ContentType);
}
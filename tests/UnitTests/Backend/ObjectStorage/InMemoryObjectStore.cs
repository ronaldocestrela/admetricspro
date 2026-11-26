using BuildingBlocks.Application.Storage;
using BuildingBlocks.Domain.Primitives;

namespace UnitTests.Backend.ObjectStorage;

/// <summary>
/// Implementação em memória de <see cref="IObjectStore"/> apenas para testes unitários (Fase 2).
/// </summary>
public sealed class InMemoryObjectStore : IObjectStore
{
    private readonly Dictionary<(string Bucket, string Key), StoredObject> _objects = new();

    /// <summary>
    /// Chaves gravadas (somente testes).
    /// </summary>
    public IReadOnlyCollection<(string Bucket, string Key)> Keys => _objects.Keys;

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

    /// <inheritdoc />
    public Task<Result<long?>> GetContentLengthAsync(
        string bucket,
        string key,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_objects.TryGetValue((bucket, key), out var stored))
        {
            return Task.FromResult(Result<long?>.Success(null));
        }

        return Task.FromResult(Result<long?>.Success(stored.Content.LongLength));
    }

    /// <inheritdoc />
    public Task<Result> DeletePrefixAsync(
        string bucket,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = prefix.Trim().TrimEnd('/') + "/";
        var keys = _objects.Keys
            .Where(k => string.Equals(k.Bucket, bucket, StringComparison.Ordinal)
                        && k.Key.StartsWith(normalized, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keys)
        {
            _objects.Remove(key);
        }

        return Task.FromResult(Result.Success());
    }

    private sealed record StoredObject(byte[] Content, string ContentType);
}
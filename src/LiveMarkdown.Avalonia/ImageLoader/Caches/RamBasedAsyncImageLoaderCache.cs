using System.Security.Cryptography;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// An in-memory image byte cache backed by weak references.
/// </summary>
public class RamBasedAsyncImageLoaderCache : AsyncImageLoaderCache
{
    /// <summary>
    /// Gets a shared in-memory cache instance.
    /// </summary>
    public static RamBasedAsyncImageLoaderCache Shared { get; } = new();

    /// <summary>
    /// Gets or sets the maximum cached entry size in bytes. Entries larger than
    /// this value are ignored. Set to 0 or a negative value to disable the limit.
    /// </summary>
    public static long MaxEntrySizeBytes { get; set; } = 32L * 1024L * 1024L;

    private readonly Dictionary<Uri, WeakReference<RamCacheEntry>> _cache = new();

    private int _checkThreshold = 16;

    /// <inheritdoc/>
    public override Task<AsyncImageLoaderCacheEntry?> GetAsync(Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cache)
        {
            if (_cache.TryGetValue(uri, out var weakRef) && weakRef.TryGetTarget(out var cachedEntry))
            {
                cachedEntry.Metadata.LastAccessedAt = DateTimeOffset.UtcNow;
                return Task.FromResult<AsyncImageLoaderCacheEntry?>(
                    new AsyncImageLoaderCacheEntry(new MemoryStream(cachedEntry.Bytes, false), cachedEntry.Metadata.Clone()));
            }

            return Task.FromResult<AsyncImageLoaderCacheEntry?>(null);
        }
    }

    /// <inheritdoc/>
    public override async Task SetAsync(Uri uri, Stream stream, AsyncImageLoaderCacheMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (metadata.NoStore) return;

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        if (MaxEntrySizeBytes > 0 && memoryStream.Length > MaxEntrySizeBytes) return;

        var bytes = memoryStream.ToArray();
        var now = DateTimeOffset.UtcNow;
        var storedMetadata = metadata.Clone();
        storedMetadata.SourceUri = uri.OriginalString;
        storedMetadata.StoredAt = now;
        storedMetadata.LastAccessedAt = now;
        storedMetadata.Length = bytes.LongLength;
        storedMetadata.BodyHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        lock (_cache)
        {
            _cache[uri] = new WeakReference<RamCacheEntry>(new RamCacheEntry(bytes, storedMetadata));
            CleanDeadEntriesIfNeeded();
        }
    }

    /// <inheritdoc/>
    public override Task TouchAsync(Uri uri, AsyncImageLoaderCacheMetadata metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cache)
        {
            if (_cache.TryGetValue(uri, out var weakRef) && weakRef.TryGetTarget(out var cachedEntry))
            {
                cachedEntry.Metadata.UpdateFromValidation(metadata);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task RemoveAsync(Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cache)
        {
            _cache.Remove(uri);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task ClearAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_cache)
        {
            _cache.Clear();
        }

        return Task.CompletedTask;
    }

    private void CleanDeadEntriesIfNeeded()
    {
        if (_cache.Count <= _checkThreshold) return;

        var keysToRemove = _cache.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
        }

        if (_cache.Count > _checkThreshold) _checkThreshold *= 2;
        else if (_cache.Count < _checkThreshold / 4) _checkThreshold = Math.Max(16, _checkThreshold / 2);
    }

    private sealed record RamCacheEntry(byte[] Bytes, AsyncImageLoaderCacheMetadata Metadata);
}

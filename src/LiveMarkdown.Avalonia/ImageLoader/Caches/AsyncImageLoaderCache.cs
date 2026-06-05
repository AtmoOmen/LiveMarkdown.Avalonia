using System.ComponentModel;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Abstract base class for asynchronous image loader caches.
/// </summary>
[TypeConverter(typeof(AsyncImageLoaderCacheTypeConverter))]
public abstract class AsyncImageLoaderCache
{
    /// <summary>
    /// Retrieves a cached image for the given URI, if available and valid.
    /// </summary>
    /// <param name="uri">The source URI to look up.</param>
    /// <param name="cancellationToken">A token that cancels the cache lookup.</param>
    /// <returns>
    /// A cache entry whose stream is owned by the caller and must be disposed, or
    /// null when the cache has no usable bytes for <paramref name="uri"/>.
    /// </returns>
    public abstract Task<AsyncImageLoaderCacheEntry?> GetAsync(Uri uri, CancellationToken cancellationToken);

    /// <summary>
    /// Caches the image data from the given stream for the specified URI, along with metadata.
    /// </summary>
    /// <param name="uri">The source URI associated with the bytes.</param>
    /// <param name="stream">The readable image byte stream. Implementations should not dispose this stream.</param>
    /// <param name="metadata">Metadata associated with the bytes.</param>
    /// <param name="cancellationToken">A token that cancels the cache write.</param>
    /// <returns>A task that completes when the cache write has finished or been skipped.</returns>
    public abstract Task SetAsync(Uri uri, Stream stream, AsyncImageLoaderCacheMetadata metadata, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the metadata of the cached image for the given URI, if it exists.
    /// </summary>
    /// <param name="uri">The source URI associated with the cached entry.</param>
    /// <param name="metadata">Updated metadata to merge into the cache entry.</param>
    /// <param name="cancellationToken">A token that cancels the metadata update.</param>
    /// <returns>A task that completes when the metadata update has finished or been skipped.</returns>
    public virtual Task TouchAsync(
        Uri uri,
        AsyncImageLoaderCacheMetadata metadata,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Removes the cached image for the given URI, if it exists.
    /// </summary>
    /// <param name="uri">The source URI to remove.</param>
    /// <param name="cancellationToken">A token that cancels the remove operation.</param>
    /// <returns>A task that completes when the entry has been removed or the cache had no entry.</returns>
    public virtual Task RemoveAsync(
        Uri uri,
        CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Clears all cached images.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels the clear operation.</param>
    /// <returns>A task that completes when the cache has been cleared.</returns>
    public virtual Task ClearAsync(
        CancellationToken cancellationToken) => Task.CompletedTask;
}

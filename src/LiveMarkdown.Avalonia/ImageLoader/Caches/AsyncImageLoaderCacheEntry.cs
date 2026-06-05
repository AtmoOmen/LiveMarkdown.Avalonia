namespace LiveMarkdown.Avalonia;

/// <summary>
/// Represents image bytes read from an <see cref="AsyncImageLoaderCache"/>.
/// </summary>
public sealed class AsyncImageLoaderCacheEntry : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncImageLoaderCacheEntry"/> class.
    /// </summary>
    /// <param name="stream">The cached image byte stream owned by this entry.</param>
    /// <param name="metadata">Metadata associated with the cached bytes.</param>
    public AsyncImageLoaderCacheEntry(Stream stream, AsyncImageLoaderCacheMetadata metadata)
    {
        Stream = stream;
        Metadata = metadata;
    }

    /// <summary>
    /// Gets the byte stream of the cached image. Dispose this entry when the
    /// stream is no longer needed.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Gets metadata associated with the cached image bytes.
    /// </summary>
    public AsyncImageLoaderCacheMetadata Metadata { get; }

    /// <inheritdoc/>
    public void Dispose() => Stream.Dispose();

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
    }
}

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Represents the result returned by an <see cref="AsyncImageLoaderHandler"/>.
/// </summary>
public sealed class AsyncImageLoaderResponse : IDisposable, IAsyncDisposable
{
    private readonly IDisposable? _owner;
    private readonly IAsyncDisposable? _asyncOwner;

    private AsyncImageLoaderResponse(
        Stream? stream,
        AsyncImageLoaderCacheMetadata metadata,
        bool isNotModified,
        IDisposable? owner = null,
        IAsyncDisposable? asyncOwner = null)
    {
        Stream = stream;
        Metadata = metadata;
        IsNotModified = isNotModified;
        _owner = owner;
        _asyncOwner = asyncOwner;
    }

    /// <summary>
    /// Gets the response byte stream. The caller owns this response and must dispose
    /// the <see cref="AsyncImageLoaderResponse"/> when finished with the stream.
    /// </summary>
    public Stream? Stream { get; }

    /// <summary>
    /// Gets metadata associated with this response.
    /// </summary>
    public AsyncImageLoaderCacheMetadata Metadata { get; }

    /// <summary>
    /// Gets whether the handler determined that cached bytes are still valid and no
    /// replacement stream was returned.
    /// </summary>
    public bool IsNotModified { get; }

    /// <summary>
    /// Creates a response that contains a byte stream.
    /// </summary>
    /// <param name="stream">The response stream owned by the returned response.</param>
    /// <param name="metadata">Metadata associated with the response.</param>
    /// <param name="owner">An optional disposable owner to release with the response.</param>
    /// <returns>A response containing <paramref name="stream"/>.</returns>
    public static AsyncImageLoaderResponse FromStream(Stream stream, AsyncImageLoaderCacheMetadata metadata, IDisposable? owner = null) =>
        new(stream, metadata, false, owner);

    /// <summary>
    /// Creates a response that indicates cached bytes have not changed.
    /// </summary>
    /// <param name="metadata">Metadata returned by the validation request.</param>
    /// <returns>A response without a byte stream.</returns>
    public static AsyncImageLoaderResponse NotModified(AsyncImageLoaderCacheMetadata metadata) =>
        new(null, metadata, true);

    /// <inheritdoc/>
    public void Dispose()
    {
        Stream?.Dispose();
        _owner?.Dispose();
        _asyncOwner?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Stream is not null)
        {
            await Stream.DisposeAsync();
        }

        if (_asyncOwner is not null)
        {
            await _asyncOwner.DisposeAsync();
        }

        _owner?.Dispose();
    }
}

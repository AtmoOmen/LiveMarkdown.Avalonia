namespace LiveMarkdown.Avalonia;

/// <summary>
/// Handler for loading images from specific URI schemes.
/// </summary>
public abstract class AsyncImageLoaderHandler
{
    /// <summary>
    /// Supported URI schemes (e.g., "http", "https", "file").
    /// </summary>
    public abstract IEnumerable<string> SupportedSchemes { get; }

    public abstract Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken);
}
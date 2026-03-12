namespace LiveMarkdown.Avalonia;

/// <summary>
/// Handler for loading images from local file URIs (filed://)
/// </summary>
public class LocalFileAsyncImageLoaderHandler : AsyncImageLoaderHandler
{
    public static LocalFileAsyncImageLoaderHandler Shared { get; } = new();

    public override IEnumerable<string> SupportedSchemes => ["file"];

    private LocalFileAsyncImageLoaderHandler() { }

    public override Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!uri.IsFile) throw new NotSupportedException("Only file URIs are supported.");

        Stream stream = File.OpenRead(uri.LocalPath);
        return Task.FromResult(stream);
    }
}
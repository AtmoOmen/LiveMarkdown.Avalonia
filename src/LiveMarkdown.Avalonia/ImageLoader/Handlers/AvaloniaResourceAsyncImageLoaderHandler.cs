using Avalonia.Platform;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Handler for loading images from Avalonia Resources (avares://)
/// </summary>
public class AvaloniaResourceAsyncImageLoaderHandler : AsyncImageLoaderHandler
{
    public static AvaloniaResourceAsyncImageLoaderHandler Shared { get; } = new();

    public override IEnumerable<string> SupportedSchemes => ["avares"];

    private AvaloniaResourceAsyncImageLoaderHandler() { }

    public override Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken) =>
        uri.Scheme != "avares" ?
            throw new NotSupportedException("Only avares URIs are supported.") :
            Task.FromResult(AssetLoader.Open(uri));
}
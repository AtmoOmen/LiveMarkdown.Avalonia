using System.Text;

namespace LiveMarkdown.Avalonia;

public class RawAsyncImageLoaderHandler : AsyncImageLoaderHandler
{
    public static RawAsyncImageLoaderHandler Shared { get; } = new();

    public override IEnumerable<string> SupportedSchemes => [string.Empty];

    private RawAsyncImageLoaderHandler() { }

    public override Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken)
    {
        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(uri.OriginalString)));
    }
}
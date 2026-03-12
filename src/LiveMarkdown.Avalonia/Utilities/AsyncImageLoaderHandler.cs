using System.Text;
using Avalonia.Platform;

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

/// <summary>
/// Handler for loading images over HTTP and HTTPS.
/// Supports redirects.
/// </summary>
/// <param name="httpClient"></param>
public class HttpAsyncImageLoaderHandler(HttpClient httpClient) : AsyncImageLoaderHandler
{
    public static HttpAsyncImageLoaderHandler Shared { get; } = new();

    public override IEnumerable<string> SupportedSchemes => ["http", "https"];

    private HttpAsyncImageLoaderHandler() : this(new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })) { }

    public override async Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (uri.Scheme != "http" && uri.Scheme != "https") throw new NotSupportedException("Only HTTP and HTTPS URIs are supported.");

        var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

#if NETSTANDARD2_0
        return await response.Content.ReadAsStreamAsync();
#else
        return await response.Content.ReadAsStreamAsync(cancellationToken);
#endif
    }
}

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

public class DataUrlAsyncImageLoaderHandler : AsyncImageLoaderHandler
{
    public static DataUrlAsyncImageLoaderHandler Shared { get; } = new();

    public override IEnumerable<string> SupportedSchemes => ["data"];

    private DataUrlAsyncImageLoaderHandler() { }

    public override Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (uri.Scheme != "data") throw new NotSupportedException("Only data URIs are supported.");

        var dataPart = uri.OriginalString.AsSpan(5);
        var commaIndex = dataPart.IndexOf(',');
        if (commaIndex == -1) throw new FormatException("Invalid Data URL: missing comma.");

        var header = dataPart[..commaIndex];
        var payload = dataPart[(commaIndex + 1)..];

        if (header.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
#if NETSTANDARD2_0
            var bytes = Convert.FromBase64CharArray(payload.ToArray(), 0, payload.Length);
            return Task.FromResult<Stream>(new MemoryStream(bytes));
#else
            var bufferSize = payload.Length * 3 / 4;
            var buffer = new byte[bufferSize];

            return Convert.TryFromBase64Chars(payload, buffer, out var bytesWritten) ?
                Task.FromResult<Stream>(new MemoryStream(buffer, 0, bytesWritten)) :
                throw new FormatException("Invalid Base64 data in Data URL.");
#endif
        }

        {
            // fallback to normal URL decoding for non-base64 data URIs
            var decoded = Uri.UnescapeDataString(payload.ToString());
            var bytes = Encoding.UTF8.GetBytes(decoded);
            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }
    }
}

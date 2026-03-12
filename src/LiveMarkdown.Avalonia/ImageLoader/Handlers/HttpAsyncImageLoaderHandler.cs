namespace LiveMarkdown.Avalonia;

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
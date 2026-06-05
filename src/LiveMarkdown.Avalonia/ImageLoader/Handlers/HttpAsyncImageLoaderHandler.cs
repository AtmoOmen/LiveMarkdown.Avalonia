using System.Net;
using System.Net.Http.Headers;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Loads image bytes over HTTP and HTTPS.
/// </summary>
/// <param name="httpClient">The HTTP client used to send image requests.</param>
public class HttpAsyncImageLoaderHandler(HttpClient httpClient) : AsyncImageLoaderHandler
{
    /// <summary>
    /// Gets a shared HTTP image loader handler using a redirect-enabled <see cref="HttpClient"/>.
    /// </summary>
    public static HttpAsyncImageLoaderHandler Shared { get; } = new();

    /// <inheritdoc/>
    public override IEnumerable<string> SupportedSchemes => ["http", "https"];

    /// <summary>
    /// Gets or sets whether HTTP conditional request headers should be sent when
    /// cached metadata contains validators. The default is true.
    /// </summary>
    public bool EnableConditionalRequests { get; set; } = true;

    private HttpAsyncImageLoaderHandler() : this(new HttpClient(new HttpClientHandler { AllowAutoRedirect = true })) { }

    /// <inheritdoc/>
    public override async Task<Stream> LoadAsync(Uri uri, CancellationToken cancellationToken)
    {
        await using var response = await LoadAsync(uri, null, cancellationToken);
        if (response.Stream is null) return Stream.Null;

        var memoryStream = new MemoryStream();
        await response.Stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <inheritdoc/>
    public override async Task<AsyncImageLoaderResponse> LoadAsync(
        Uri uri,
        AsyncImageLoaderCacheMetadata? cachedMetadata,
        CancellationToken cancellationToken)
    {
        if (uri.Scheme != "http" && uri.Scheme != "https") throw new NotSupportedException("Only HTTP and HTTPS URIs are supported.");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        ApplyValidators(request, cachedMetadata);

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var metadata = CreateMetadata(uri, response);

        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            response.Dispose();
            return AsyncImageLoaderResponse.NotModified(metadata);
        }

        try
        {
            response.EnsureSuccessStatusCode();

#if NETSTANDARD2_0
            var stream = await response.Content.ReadAsStreamAsync();
#else
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#endif
            return AsyncImageLoaderResponse.FromStream(stream, metadata, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private void ApplyValidators(HttpRequestMessage request, AsyncImageLoaderCacheMetadata? metadata)
    {
        if (!EnableConditionalRequests || metadata is null) return;

        var etag = metadata.GetHttpETag();
        if (!string.IsNullOrWhiteSpace(etag))
        {
            if (EntityTagHeaderValue.TryParse(etag, out var entityTag))
            {
                request.Headers.IfNoneMatch.Add(entityTag);
            }
            else
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", etag);
            }
        }

        if (metadata.GetHttpLastModified() is { } lastModified)
        {
            request.Headers.IfModifiedSince = lastModified;
        }
    }

    private static AsyncImageLoaderCacheMetadata CreateMetadata(Uri uri, HttpResponseMessage response)
    {
        var now = DateTimeOffset.UtcNow;
        var cacheControl = response.Headers.CacheControl;
        var metadata = AsyncImageLoaderCacheMetadata.Create(uri);

        metadata.CreatedAt = now;
        metadata.StoredAt = now;
        metadata.LastAccessedAt = now;
        metadata.Length = response.Content.Headers.ContentLength;
        metadata.NoStore = cacheControl?.NoStore == true;
        metadata.NoCache = cacheControl?.NoCache == true;
        metadata.SetHttpCacheControl(cacheControl?.ToString());
        metadata.SetHttpETag(response.Headers.ETag?.ToString());
        metadata.SetHttpLastModified(response.Content.Headers.LastModified);
        metadata.SetContentType(response.Content.Headers.ContentType?.ToString());

        if (cacheControl?.MaxAge is { } maxAge)
        {
            metadata.ExpiresAt = now.Add(maxAge);
        }
        else if (response.Content.Headers.Expires is { } expires)
        {
            metadata.ExpiresAt = expires;
        }

        return metadata;
    }
}

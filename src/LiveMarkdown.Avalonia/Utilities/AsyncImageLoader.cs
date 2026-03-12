using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Asynchronously loads images from a given source URL and caches them.
/// </summary>
public class AsyncImageLoader
{
    /// <summary>
    /// Attached property for the image source URL.
    /// </summary>
    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<AsyncImageLoader, Image, string?>("Source");

    /// <summary>
    /// Sets the source URL for the image.
    /// </summary>
    public static void SetSource(Image obj, string? value) => obj.SetValue(SourceProperty, value);

    /// <summary>
    /// Gets the source URL for the image.
    /// </summary>
    public static string? GetSource(Image obj) => obj.GetValue(SourceProperty);

    /// <summary>
    /// Attached property for the SizeToContent behavior.
    /// </summary>
    public static readonly AttachedProperty<SizeToContent> SizeToContentProperty =
        AvaloniaProperty.RegisterAttached<AsyncImageLoader, Image, SizeToContent>("SizeToContent", SizeToContent.WidthAndHeight);

    /// <summary>
    /// Sets the SizeToContent behavior for the image.
    /// Indicates whether the image should size itself to its content by setting Width and Height.
    /// </summary>
    public static void SetSizeToContent(Image obj, SizeToContent value) => obj.SetValue(SizeToContentProperty, value);

    /// <summary>
    /// Gets the SizeToContent behavior for the image.
    /// </summary>
    public static SizeToContent GetSizeToContent(Image obj) => obj.GetValue(SizeToContentProperty);

    /// <summary>
    /// Attached property for the image cache.
    /// </summary>
    public static readonly AttachedProperty<AsyncImageLoaderCache?> CacheProperty =
        AvaloniaProperty.RegisterAttached<AsyncImageLoader, Image, AsyncImageLoaderCache?>("Cache", RamBasedAsyncImageLoaderCache.Shared);

    /// <summary>
    /// Sets the cache for the image loader.
    /// You can use one of the built-in caches (convert from string), or implement your own.
    /// built-in caches include: `None`, `Ram`. Default is `Ram`.
    /// </summary>
    public static void SetCache(Image obj, AsyncImageLoaderCache? value) =>
        obj.SetValue(CacheProperty, value);

    /// <summary>
    /// Gets the cache for the image loader.
    /// </summary>
    public static AsyncImageLoaderCache? GetCache(Image obj) => obj.GetValue(CacheProperty);

    /// <summary>
    /// Attached property for custom image loaders.
    /// </summary>
    public static readonly AttachedProperty<IReadOnlyCollection<AsyncImageLoaderHandler>> HandlersProperty =
        AvaloniaProperty.RegisterAttached<AsyncImageLoader, Image, IReadOnlyCollection<AsyncImageLoaderHandler>>(
            "Handlers",
            [
                HttpAsyncImageLoaderHandler.Shared,
                LocalFileAsyncImageLoaderHandler.Shared,
                AvaloniaResourceAsyncImageLoaderHandler.Shared
            ]);

    /// <summary>
    /// Sets the custom image loader handlers.
    /// You can add your own handlers to support custom URI schemes.
    /// Default is `Http`, `LocalFile`, `AvaloniaResource`. (HTTP supports redirects and HTTPS)
    /// </summary>
    public static void SetHandlers(Image obj, IReadOnlyCollection<AsyncImageLoaderHandler> value) => obj.SetValue(HandlersProperty, value);

    /// <summary>
    /// Gets the custom image loader handlers.
    /// </summary>
    public static IReadOnlyCollection<AsyncImageLoaderHandler> GetHandlers(Image obj) => obj.GetValue(HandlersProperty);

    /// <summary>
    /// Attached property for image decoders. If null, the DefaultDecoders will be used.
    /// </summary>
    public static readonly AttachedProperty<IReadOnlyCollection<IImageDecoder>?> DecodersProperty =
        AvaloniaProperty.RegisterAttached<AsyncImageLoader, Image, IReadOnlyCollection<IImageDecoder>?>("Decoders");

    /// <summary>
    /// Sets the image decoders used to parse the stream into an IImage.
    /// Evaluated in order until one returns a non-null IImage.
    /// </summary>
    public static void SetDecoders(Image obj, IReadOnlyCollection<IImageDecoder>? value) => obj.SetValue(DecodersProperty, value);

    /// <summary>
    /// Gets the image decoders.
    /// </summary>
    public static IReadOnlyCollection<IImageDecoder>? GetDecoders(Image obj) => obj.GetValue(DecodersProperty);

    /// <summary>
    /// Gets or sets the default image decoders used if an Image does not have its own decoders set.
    /// </summary>
    public static IReadOnlyCollection<IImageDecoder> DefaultDecoders { get; set; } = [DefaultBitmapDecoder.Shared];

    private readonly static Dictionary<Image, (Task task, CancellationTokenSource cts)> ImageLoadTasks = new();

    static AsyncImageLoader()
    {
        SourceProperty.Changed.AddClassHandler<Image>(HandleSourceChanged);
    }

    private static void HandleSourceChanged(Image sender, AvaloniaPropertyChangedEventArgs args)
    {
        // This method is always called on the UI thread, so we can safely access the UI elements.
        if (ImageLoadTasks.TryGetValue(sender, out var pair))
        {
            pair.cts.Cancel(); // Cancel the previous loading task if it exists
            ImageLoadTasks.Remove(sender);
        }

        var newSource = args.NewValue as string;
        if (!Uri.TryCreate(newSource, UriKind.RelativeOrAbsolute, out var uri))
        {
            sender.Source = null; // Clear the image source if the new value is not a valid URI
            ApplySizeToContent(sender, null);
            return;
        }

        var cache = GetCache(sender);
        if (cache?.GetImage(uri) is { } cachedImage)
        {
            sender.Source = cachedImage; // Use the cached image if available
            ApplySizeToContent(sender, cachedImage);
            return;
        }

        var handler = GetHandlers(sender).FirstOrDefault(h => h.SupportedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase));
        if (handler is null)
        {
            sender.Source = null; // No handler found for the URI scheme
            ApplySizeToContent(sender, null);
            return;
        }

        var decoders = GetDecoders(sender) ?? DefaultDecoders;
        var newPair = CreateLoadPair(sender, uri, handler, decoders, cache);
        ImageLoadTasks.Add(sender, newPair);
    }

    private static void ApplySizeToContent(Image image, IImage? loadedImage)
    {
        var sizeToContent = GetSizeToContent(image);
        if (sizeToContent == SizeToContent.Manual) return;

        var (imageWidth, imageHeight) = loadedImage switch
        {
            Bitmap bitmap => (bitmap.PixelSize.Width, bitmap.PixelSize.Height),
            // The core does not know about SvgImage anymore, so SvgImages will return (Size.Width, Size.Height) via their IImage interface properties if possible, or we handle it via IImage properties.
            // Since IImage only exposes Size, we use that universally.
            not null => (loadedImage.Size.Width, loadedImage.Size.Height),
            _ => (double.NaN, double.NaN)
        };

#if NETSTANDARD2_0
        if (double.IsInfinity(imageWidth) || double.IsNaN(imageWidth) || imageWidth <= 1d ||
            double.IsInfinity(imageHeight) || double.IsNaN(imageHeight) || imageHeight <= 1d)
#else
        if (double.IsSubnormal(imageWidth) || imageWidth <= 1d || double.IsSubnormal(imageHeight) || imageHeight <= 1d)
#endif
        {
            image.Width = double.NaN;
            image.Height = double.NaN;
            return;
        }

        var actualWidth = Math.Min(imageWidth, image.MaxWidth);
        var actualHeight = Math.Min(imageHeight, image.MaxHeight);

        // Ensure aspect ratio is maintained
        var widthRatio = actualWidth / imageWidth;
        var heightRatio = actualHeight / imageHeight;
        var minRatio = Math.Min(widthRatio, heightRatio);
        actualWidth = imageWidth * minRatio;
        actualHeight = imageHeight * minRatio;

        image.Width = sizeToContent.HasFlag(SizeToContent.Width) ? actualWidth : double.NaN;
        image.Height = sizeToContent.HasFlag(SizeToContent.Height) ? actualHeight : double.NaN;
    }

    private static (Task task, CancellationTokenSource cts) CreateLoadPair(
        Image image,
        Uri uri,
        AsyncImageLoaderHandler handler,
        IReadOnlyCollection<IImageDecoder> decoders,
        AsyncImageLoaderCache? cache)
    {
        var cts = new CancellationTokenSource();
        var task = Task.Run(
                async () =>
                {
                    try
                    {
#if NETSTANDARD2_0
                        using var stream = await handler.LoadAsync(uri, cts.Token);
#else
                        await using var stream = await handler.LoadAsync(uri, cts.Token);
#endif

                        if (stream.Length == 0) return null;

                        Stream seekableStream;
                        if (stream.CanSeek)
                        {
                            seekableStream = stream;
                        }
                        else
                        {
                            seekableStream = new MemoryStream();
#if NETSTANDARD2_0
                            await stream.CopyToAsync(seekableStream, 81920, cts.Token);
#else
                            await stream.CopyToAsync(seekableStream, cts.Token);
#endif
                        }

#if NETSTANDARD2_0
                        using (seekableStream)
#else
                        await using (seekableStream)
#endif
                        {
                            foreach (var decoder in decoders)
                            {
                                cts.Token.ThrowIfCancellationRequested();
                                seekableStream.Position = 0; // Reset stream position for each decoder

                                var result = await decoder.TryDecodeAsync(image, seekableStream, uri, cts.Token);
                                if (result is not null)
                                {
                                    return result;
                                }
                            }
                        }

                        return null; // All decoders failed
                    }
                    catch (OperationCanceledException)
                    {
                        // Task was canceled, do nothing
                        throw;
                    }
                    catch
                    {
                        // Handle other exceptions as needed
                        return null; // Clear the image source on error
                    }
                },
                cts.Token)
            .ContinueWith(
                t =>
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (ImageLoadTasks.TryGetValue(image, out var pair) && pair.cts == cts)
                        {
                            ImageLoadTasks.Remove(image); // Remove the task from the dictionary
                        }

                        if (t.Exception is not null) return; // Operation was canceled or failed, do nothing

                        var result = t.Result;
                        if (result is not null) cache?.SetImage(uri, result);

                        image.Source = result;
                        ApplySizeToContent(image, result);
                    });
                },
                cts.Token);

        return (task, cts);
    }
}

#if NETSTANDARD2_0
public class AsyncImageLoaderCacheTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object? value)
    {
        if (value is string str)
        {
            return str switch
            {
                "None" => null,
                "Ram" => RamBasedAsyncImageLoaderCache.Shared,
                _ => throw new NotSupportedException($"Cache type '{str}' is not supported.")
            };
        }

        return base.ConvertFrom(context, culture, value);
    }
}
#else
public class AsyncImageLoaderCacheTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return str switch
            {
                "None" => null,
                "Ram" => RamBasedAsyncImageLoaderCache.Shared,
                _ => throw new NotSupportedException($"Cache type '{str}' is not supported.")
            };
        }

        return base.ConvertFrom(context, culture, value);
    }
}
#endif

/// <summary>
/// Represents a decoder that can translate a Stream into an Avalonia IImage.
/// </summary>
public interface IImageDecoder
{
    /// <summary>
    /// Attempts to decode the stream. Returns null if the format is unsupported or invalid.
    /// </summary>
    /// <param name="target">The target Image control, useful for reading attached properties.</param>
    /// <param name="stream">The seekable image stream.</param>
    /// <param name="uri">The source URI of the image.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An IImage if decoding is successful, otherwise null.</returns>
    Task<IImage?> TryDecodeAsync(Image target, Stream stream, Uri uri, CancellationToken cancellationToken);
}

/// <summary>
/// The default decoder that attempts to load the stream as a standard Avalonia Bitmap.
/// </summary>
public class DefaultBitmapDecoder : IImageDecoder
{
    public static DefaultBitmapDecoder Shared { get; } = new();

    public Task<IImage?> TryDecodeAsync(Image target, Stream stream, Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            var bitmap = new Bitmap(stream);
            return Task.FromResult<IImage?>(bitmap);
        }
        catch
        {
            return Task.FromResult<IImage?>(null);
        }
    }
}

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
/// Handler for loading images from local file URIs.
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

[TypeConverter(typeof(AsyncImageLoaderCacheTypeConverter))]
public abstract class AsyncImageLoaderCache
{
    public abstract IImage? GetImage(Uri uri);

    public abstract void SetImage(Uri uri, IImage image);
}

public class RamBasedAsyncImageLoaderCache : AsyncImageLoaderCache
{
    public static RamBasedAsyncImageLoaderCache Shared { get; } = new();

    private readonly Dictionary<Uri, WeakReference<IImage>> _cache = new();

    private int _checkThreshold = 16;

    public override IImage? GetImage(Uri uri)
    {
        lock (_cache)
        {
            if (_cache.TryGetValue(uri, out var weakRef) && weakRef.TryGetTarget(out var image))
            {
                return image;
            }

            return null;
        }
    }

    public override void SetImage(Uri uri, IImage image)
    {
        lock (_cache)
        {
            _cache[uri] = new WeakReference<IImage>(image);

            if (_cache.Count <= _checkThreshold) return;

            // Clean up weak references that are no longer alive
            var keysToRemove = _cache.Where(kvp => !kvp.Value.TryGetTarget(out _)).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            if (_cache.Count > _checkThreshold) _checkThreshold *= 2;
            else if (_cache.Count < _checkThreshold / 4) _checkThreshold /= 2;
        }
    }
}
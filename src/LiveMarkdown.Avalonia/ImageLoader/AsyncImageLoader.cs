using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Asynchronously loads images from a given source URL and caches them.
/// </summary>
public static class AsyncImageLoader
{
    /// <summary>
    /// Attached property for the image source URL.
    /// </summary>
    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Image, Image, string?>("Source");

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
        AvaloniaProperty.RegisterAttached<Image, Image, SizeToContent>("SizeToContent", SizeToContent.WidthAndHeight);

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
        AvaloniaProperty.RegisterAttached<Image, Image, AsyncImageLoaderCache?>("Cache");

    /// <summary>
    /// Sets the cache for the image loader.
    /// You can use one of the built-in caches (convert from string), or implement your own.
    /// built-in caches include: `None`, `Ram`, `File` and `Disk`. Default is `Ram`.
    /// </summary>
    public static void SetCache(Image obj, AsyncImageLoaderCache? value) => obj.SetValue(CacheProperty, value);

    /// <summary>
    /// Gets the cache for the image loader.
    /// </summary>
    public static AsyncImageLoaderCache? GetCache(Image obj) => obj.GetValue(CacheProperty);

    /// <summary>
    /// Gets or sets the default cache used if an Image does not have its own cache set.
    /// </summary>
    public static AsyncImageLoaderCache? DefaultCache { get; set; } = RamBasedAsyncImageLoaderCache.Shared;

    /// <summary>
    /// Attached property for custom image loaders.
    /// </summary>
    public static readonly AttachedProperty<IReadOnlyCollection<AsyncImageLoaderHandler>> HandlersProperty =
        AvaloniaProperty.RegisterAttached<Image, Image, IReadOnlyCollection<AsyncImageLoaderHandler>>(
            "Handlers",
            [
                HttpAsyncImageLoaderHandler.Shared,
                LocalFileAsyncImageLoaderHandler.Shared,
                AvaloniaResourceAsyncImageLoaderHandler.Shared,
                DataUrlAsyncImageLoaderHandler.Shared,
                RawAsyncImageLoaderHandler.Shared
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
        AvaloniaProperty.RegisterAttached<Image, Image, IReadOnlyCollection<IImageDecoder>?>("Decoders");

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
            pair.cts.Cancel();
            ImageLoadTasks.Remove(sender);
        }

        var newSource = args.NewValue as string;
        if (!Uri.TryCreate(newSource, UriKind.RelativeOrAbsolute, out var uri))
        {
            sender.Source = null;
            ApplySizeToContent(sender, null);
            return;
        }

        var scheme = uri.IsAbsoluteUri ? uri.Scheme : string.Empty;
        var handler = GetHandlers(sender).FirstOrDefault(h => h.SupportedSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase));
        if (handler is null)
        {
            sender.Source = null;
            ApplySizeToContent(sender, null);
            return;
        }

        var decoders = GetDecoders(sender) ?? DefaultDecoders;
        var cache = GetCache(sender) ?? DefaultCache;
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
        var task = Task.Run(() => LoadAndApplyAsync(image, uri, handler, decoders, cache, cts), CancellationToken.None);
        return (task, cts);
    }

    private static async Task LoadAndApplyAsync(
        Image image,
        Uri uri,
        AsyncImageLoaderHandler handler,
        IReadOnlyCollection<IImageDecoder> decoders,
        AsyncImageLoaderCache? cache,
        CancellationTokenSource cts)
    {
        IImage? result = null;
        var shouldApply = false;

        try
        {
            result = await LoadImageAsync(image, uri, handler, decoders, cache, cts.Token);
            shouldApply = true;
        }
        catch (OperationCanceledException)
        {
            // A newer source replaced this request.
        }
        catch
        {
            shouldApply = true;
        }

        try
        {
            await Dispatcher.UIThread.InvokeAsync(
                () =>
                {
                    if (!ImageLoadTasks.TryGetValue(image, out var pair) || pair.cts != cts)
                    {
                        return;
                    }

                    ImageLoadTasks.Remove(image);

                    if (shouldApply && !cts.IsCancellationRequested)
                    {
                        image.Source = result;
                        ApplySizeToContent(image, result);
                    }
                });
        }
        finally
        {
            cts.Dispose();
        }
    }

    private static async Task<IImage?> LoadImageAsync(
        Image image,
        Uri uri,
        AsyncImageLoaderHandler handler,
        IReadOnlyCollection<IImageDecoder> decoders,
        AsyncImageLoaderCache? cache,
        CancellationToken cancellationToken)
    {
        AsyncImageLoaderCacheEntry? cachedEntry = null;
        AsyncImageLoaderResponse? response = null;

        try
        {
            cachedEntry = cache is null ? null : await cache.GetAsync(uri, cancellationToken);
            if (cachedEntry is not null && cachedEntry.Metadata.IsFresh(DateTimeOffset.UtcNow))
            {
                var cachedImage = await DecodeAsync(image, cachedEntry.Stream, uri, decoders, cancellationToken);
                if (cachedImage is not null)
                {
                    return cachedImage;
                }

                await cache!.RemoveAsync(uri, cancellationToken);
                await cachedEntry.DisposeAsync();
                cachedEntry = null;
            }

            response = await handler.LoadAsync(uri, cachedEntry?.Metadata, cancellationToken);
            if (response.IsNotModified)
            {
                if (cachedEntry is null) return null;

                if (cache is not null)
                {
                    await cache.TouchAsync(uri, response.Metadata, cancellationToken);
                }

                var cachedImage = await DecodeAsync(image, cachedEntry.Stream, uri, decoders, cancellationToken);
                if (cachedImage is null && cache is not null)
                {
                    await cache.RemoveAsync(uri, cancellationToken);
                }

                return cachedImage;
            }

            if (response.Stream is null) return null;

            await using (var seekableStream = await EnsureSeekableStreamAsync(response.Stream, cancellationToken))
            {
                if (seekableStream.Length == 0) return null;

                var decodedImage = await DecodeAsync(image, seekableStream, uri, decoders, cancellationToken);
                if (decodedImage is not null && cache is not null)
                {
                    seekableStream.Position = 0;
                    await cache.SetAsync(uri, seekableStream, response.Metadata, cancellationToken);
                }

                return decodedImage;
            }
        }
        finally
        {
            if (cachedEntry is not null)
            {
                await cachedEntry.DisposeAsync();
            }

            if (response is not null)
            {
                await response.DisposeAsync();
            }
        }
    }

    private static async Task<Stream> EnsureSeekableStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
            return stream;
        }

        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static async Task<IImage?> DecodeAsync(
        Image image,
        Stream stream,
        Uri uri,
        IReadOnlyCollection<IImageDecoder> decoders,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            await using var seekableStream = await EnsureSeekableStreamAsync(stream, cancellationToken);
            return await DecodeAsync(image, seekableStream, uri, decoders, cancellationToken);
        }

        foreach (var decoder in decoders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            stream.Position = 0;

            var result = await decoder.TryDecodeAsync(image, stream, uri, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}

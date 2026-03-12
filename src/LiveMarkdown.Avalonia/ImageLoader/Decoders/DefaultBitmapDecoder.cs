using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace LiveMarkdown.Avalonia;

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
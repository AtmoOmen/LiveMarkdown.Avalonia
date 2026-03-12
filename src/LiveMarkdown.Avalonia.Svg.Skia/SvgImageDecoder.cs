using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using Svg.Model;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Decoder for parsing SVG streams into SvgImage objects.
/// </summary>
public class SvgImageDecoder : IImageDecoder
{
    public static SvgImageDecoder Shared { get; } = new();

    private SvgImageDecoder() { }

    public async Task<IImage?> TryDecodeAsync(Image target, Stream stream, Uri uri, CancellationToken cancellationToken)
    {
        // Fast-fail check: If it's pure binary (contains null bytes in the header), it's likely a Bitmap, skip SVG parsing.
        if (!IsLikelySvg(uri, stream))
        {
            return null;
        }

        try
        {
            stream.Position = 0;
            var parameters = new SvgParameters
            {
                Css = await Dispatcher.UIThread.InvokeAsync(() => target.GetValue(SvgImageExtension.SvgCssProperty))
            };

            var svgSource = SvgSource.LoadFromStream(stream, parameters);
            return await Dispatcher.UIThread.InvokeAsync(() => new SvgImage { Source = svgSource });
        }
        catch
        {
            // Parsing failed (e.g., malformed SVG), let the next decoder try
        }

        return null;
    }

    private static bool IsLikelySvg(Uri uri, Stream stream)
    {
        if (uri.ToString().EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return true;

        var buffer = new byte[32];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        if (bytesRead == 0) return false;

        // Check for null bytes which strongly indicate non-text binary data (like PNG/JPG)
        return !buffer.Take(bytesRead).Contains((byte)0);
    }
}
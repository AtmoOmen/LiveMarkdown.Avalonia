using Avalonia.Controls;
using Avalonia.Media;

namespace LiveMarkdown.Avalonia;

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
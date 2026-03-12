using Avalonia;
using Avalonia.Controls;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Provides SVG-specific attached properties for AsyncImageLoader.
/// </summary>
public class SvgImageExtension
{
    /// <summary>
    /// Attached property for the SVG CSS styles.
    /// This only works before the image is loaded and requires SvgImageDecoder to be registered.
    /// </summary>
    public static readonly AttachedProperty<string?> SvgCssProperty =
        AvaloniaProperty.RegisterAttached<SvgImageExtension, Image, string?>("SvgCss");

    /// <summary>
    /// Sets the CSS styles for the SVG image.
    /// </summary>
    public static void SetSvgCss(Image obj, string? value) => obj.SetValue(SvgCssProperty, value);

    /// <summary>
    /// Gets the CSS styles for the SVG image.
    /// </summary>
    public static string? GetSvgCss(Image obj) => obj.GetValue(SvgCssProperty);
}
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
    public static readonly AttachedProperty<string?> CssProperty =
        AvaloniaProperty.RegisterAttached<SvgImageExtension, Image, string?>("Css");

    /// <summary>
    /// Sets the CSS styles for the SVG image.
    /// </summary>
    public static void SetCss(Image obj, string? value) => obj.SetValue(CssProperty, value);

    /// <summary>
    /// Gets the CSS styles for the SVG image.
    /// </summary>
    public static string? GetCss(Image obj) => obj.GetValue(CssProperty);
}
using System.Globalization;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Normalizes small Mermaid/CSS style fragments emitted by Mermaider before native rendering uses them.
/// </summary>
/// <remarks>
/// Mermaider intentionally keeps values close to Mermaid's CSS-like source, so a value such as
/// <c>color:#123;</c> may arrive as <c>#123;</c>. Avalonia's parsers expect cleaner values, and
/// failing to normalize here silently drops useful class/style directives back to presenter defaults.
/// </remarks>
internal static class MermaidStyleValue
{
    /// <summary>
    /// Normalizes a Mermaid color value into an Avalonia-friendly string.
    /// </summary>
    /// <remarks>
    /// CSS shorthand colors are expanded because Avalonia's parser expects full channel pairs in
    /// more cases. Four-channel CSS shorthand is converted from <c>#RGBA</c> to Avalonia's
    /// <c>#AARRGGBB</c> ordering.
    /// </remarks>
    public static string? NormalizeColor(string? value)
    {
        var normalized = NormalizeCssToken(value);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.StartsWith('#') && normalized.Length is 4 or 5)
        {
            return normalized.Length == 4
                ? $"#{normalized[1]}{normalized[1]}{normalized[2]}{normalized[2]}{normalized[3]}{normalized[3]}"
                : $"#{normalized[4]}{normalized[4]}{normalized[1]}{normalized[1]}{normalized[2]}{normalized[2]}{normalized[3]}{normalized[3]}";
        }

        return normalized;
    }

    /// <summary>
    /// Normalizes a Mermaid stroke-width value into a culture-invariant numeric string.
    /// </summary>
    /// <remarks>
    /// Mermaid style directives commonly use CSS syntax such as <c>stroke-width:2px;</c>. Native
    /// rendering only needs the device-independent number, so the optional <c>px</c> suffix is
    /// removed while unsupported units deliberately fall through and let the caller use a fallback.
    /// </remarks>
    public static string? NormalizeLength(string? value)
    {
        var normalized = NormalizeCssToken(value);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.EndsWith("px", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^2].Trim();
        }

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : normalized;
    }

    private static string? NormalizeCssToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().TrimEnd(';').Trim();
        const string important = "!important";
        if (normalized.EndsWith(important, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^important.Length].Trim().TrimEnd(';').Trim();
        }

        return normalized.Length == 0 ? null : normalized;
    }
}
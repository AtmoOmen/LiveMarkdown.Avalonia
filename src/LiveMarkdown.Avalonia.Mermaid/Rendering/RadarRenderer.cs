using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid radar charts.
/// </summary>
/// <remarks>
/// Radar chart rendering is expected to translate Mermaider's polar-style data into explicit
/// Avalonia geometry, keeping chart math separate from presenter styling.
/// </remarks>
public static class RadarRenderer
{
    /// <summary>
    /// Draws a radar chart.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, RadarChart chart)
    {

    }
}
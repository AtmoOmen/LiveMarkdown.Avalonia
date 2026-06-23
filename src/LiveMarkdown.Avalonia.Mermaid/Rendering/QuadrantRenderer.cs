using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid quadrant charts.
/// </summary>
/// <remarks>
/// Quadrant charts have chart-specific axes and region labels, so this renderer should keep those
/// decisions local while reusing shared text and geometry helpers.
/// </remarks>
public static class QuadrantRenderer
{
    /// <summary>
    /// Draws a quadrant chart.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, QuadrantChart chart)
    {

    }
}
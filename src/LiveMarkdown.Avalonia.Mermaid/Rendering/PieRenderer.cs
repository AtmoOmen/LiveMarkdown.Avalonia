using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid pie charts.
/// </summary>
/// <remarks>
/// Pie charts do not use the same positioned graph model as flowcharts, so this renderer is the
/// diagram-specific bridge from Mermaider's parsed chart data to Avalonia drawing operations.
/// </remarks>
public static class PieRenderer
{
    /// <summary>
    /// Draws a pie chart diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PieChart chart)
    {

    }
}
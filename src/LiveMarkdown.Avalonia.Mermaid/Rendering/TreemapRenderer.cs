using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid treemap diagrams.
/// </summary>
/// <remarks>
/// Treemap rendering should treat the Mermaider model as the source of hierarchy and values, then
/// derive rectangles and labels using Avalonia drawing primitives.
/// </remarks>
public static class TreemapRenderer
{
    /// <summary>
    /// Draws a treemap diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, TreemapDiagram diagram)
    {

    }
}
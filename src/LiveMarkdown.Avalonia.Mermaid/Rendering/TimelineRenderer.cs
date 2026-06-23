using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid timeline diagrams.
/// </summary>
/// <remarks>
/// Timeline rendering should keep chronology layout assumptions close to this renderer while using
/// presenter style properties for text, strokes, and fills.
/// </remarks>
public static class TimelineRenderer
{
    /// <summary>
    /// Draws a timeline diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, TimelineDiagram diagram)
    {

    }
}
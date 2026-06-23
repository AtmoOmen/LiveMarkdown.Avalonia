using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid entity-relationship diagrams.
/// </summary>
/// <remarks>
/// The parser and layout model come from Mermaider; this renderer is responsible only for translating
/// the positioned diagram into Avalonia drawing primitives.
/// </remarks>
public static class ErRenderer
{
    /// <summary>
    /// Draws a positioned entity-relationship diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedErDiagram diagram)
    {

    }
}
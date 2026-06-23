using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid class diagrams.
/// </summary>
/// <remarks>
/// This type is intentionally shaped like the flowchart renderer: Mermaider owns parsing and layout,
/// while this renderer converts the positioned model to <see cref="DrawingContext"/> calls.
/// </remarks>
public static class ClassRenderer
{
    /// <summary>
    /// Draws a positioned class diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedClassDiagram diagram)
    {

    }
}
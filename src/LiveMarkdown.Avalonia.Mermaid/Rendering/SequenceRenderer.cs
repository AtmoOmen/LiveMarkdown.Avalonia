using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid sequence diagrams.
/// </summary>
/// <remarks>
/// Sequence diagrams are expected to share text measurement and arrow helpers with flowcharts, while
/// keeping participant/lifeline/message ordering logic inside this renderer.
/// </remarks>
internal static class SequenceRenderer
{
    /// <summary>
    /// Draws a positioned sequence diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, PositionedSequenceDiagram diagram)
    {

    }
}
using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid Venn diagrams.
/// </summary>
/// <remarks>
/// Venn rendering is diagram-specific because overlap geometry and label placement do not map to the
/// flowchart edge/node helpers directly.
/// </remarks>
public static class VennRenderer
{
    /// <summary>
    /// Draws a Venn diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, VennDiagram diagram)
    {

    }
}
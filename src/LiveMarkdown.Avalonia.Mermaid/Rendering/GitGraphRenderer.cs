using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Native Avalonia renderer entry point for Mermaid git graph diagrams.
/// </summary>
/// <remarks>
/// Git graph rendering will use the parsed Mermaider model directly and should share text and
/// geometry helpers with the other native renderers.
/// </remarks>
public static class GitGraphRenderer
{
    /// <summary>
    /// Draws a git graph diagram.
    /// </summary>
    public static void Render(DrawingContext dc, MermaidPresenter presenter, GitGraph graph)
    {

    }
}
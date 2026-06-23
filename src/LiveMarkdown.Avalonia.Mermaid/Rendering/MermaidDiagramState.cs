using Avalonia;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Captures the presenter's current parsed diagram, measured size, and parse/layout failure state.
/// </summary>
/// <remarks>
/// This is intentionally a thin state object rather than a renderer abstraction. It lets
/// <see cref="MermaidPresenter"/> keep its existing switch-based dispatch while avoiding scattered
/// nullable fields for model, desired size, preprocessing metadata, and error fallback rendering.
/// </remarks>
/// <param name="DiagramType">The Mermaider diagram kind for <see cref="Diagram"/>, or <see langword="null"/> when empty or failed.</param>
/// <param name="Diagram">The positioned Mermaider diagram model consumed by the native renderer switch.</param>
/// <param name="DesiredSize">Desired control size computed from the positioned diagram or fallback error/source text.</param>
/// <param name="Error">Parse or layout exception captured for non-throwing error rendering.</param>
/// <param name="Input">Preprocessed Mermaid input associated with the state, when preprocessing completed.</param>
internal sealed record MermaidDiagramState(
    DiagramType? DiagramType,
    object? Diagram,
    Size DesiredSize,
    Exception? Error,
    MermaidInput? Input
)
{
    /// <summary>
    /// Represents the absence of diagram content.
    /// </summary>
    public static MermaidDiagramState Empty { get; } = new(null, null, default, null, null);

    /// <summary>
    /// Creates a successful parsed/layouted diagram state.
    /// </summary>
    public static MermaidDiagramState Success(DiagramType diagramType, object diagram, Size desiredSize, MermaidInput input) =>
        new(diagramType, diagram, desiredSize, null, input);

    /// <summary>
    /// Creates a failed state that can still reserve space for fallback error/source rendering.
    /// </summary>
    public static MermaidDiagramState Failed(Exception error, Size desiredSize, MermaidInput? input = null) =>
        new(null, null, desiredSize, error, input);
}
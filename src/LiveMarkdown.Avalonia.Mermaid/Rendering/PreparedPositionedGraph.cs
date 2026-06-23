using Avalonia.Media;
using Mermaider.Models;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// Stores Flowchart/State layout data plus expensive renderer-neutral derived values.
/// </summary>
/// <remarks>
/// This is intentionally not a drawing command cache. It keeps only data that is independent from
/// presenter styling, such as rounded edge geometry and flattened inline text layouts. The actual
/// rendering still uses the live <see cref="DrawingContext"/>, so clipping, rounded rectangles, pens,
/// brushes, and font settings retain normal Avalonia behavior.
/// </remarks>
internal sealed record PreparedPositionedGraph(
    PositionedGraph Graph,
    IReadOnlyList<PreparedPositionedGroup> Groups,
    IReadOnlyList<PreparedPositionedEdge> Edges,
    IReadOnlyList<PreparedPositionedNode> Nodes,
    IReadOnlyList<PreparedPositionedNote> Notes)
{
    private const double EdgeCornerRadius = 6;

    /// <summary>
    /// Builds a prepared graph from Mermaider's positioned graph.
    /// </summary>
    public static PreparedPositionedGraph Prepare(PositionedGraph graph)
    {
        return new PreparedPositionedGraph(
            graph,
            graph.Groups.Select(PrepareGroup).ToList(),
            graph.Edges.Select(PrepareEdge).ToList(),
            graph.Nodes.Select(PrepareNode).ToList(),
            graph.Notes.Select(PrepareNote).ToList());
    }

    private static PreparedPositionedGroup PrepareGroup(PositionedGroup group)
    {
        return new PreparedPositionedGroup(
            group,
            MermaidInlineTextParser.Parse(group.Label, isMarkdown: false),
            group.Children.Select(PrepareGroup).ToList());
    }

    private static PreparedPositionedEdge PrepareEdge(PositionedEdge edge)
    {
        var labelLayout = edge.Label is null
            ? null
            : MermaidInlineTextParser.Parse(edge.Label, isMarkdown: false);
        var labelPosition = edge.LabelPosition ?? MermaidDrawingHelpers.Midpoint(edge.Points);

        return new PreparedPositionedEdge(edge, EdgeCornerRadius, labelPosition, labelLayout);
    }

    private static PreparedPositionedNode PrepareNode(PositionedNode node)
    {
        return new PreparedPositionedNode(
            node,
            MermaidInlineTextParser.Parse(node.Label, node.IsMarkdown));
    }

    private static PreparedPositionedNote PrepareNote(PositionedGraphNote note)
    {
        return new PreparedPositionedNote(
            note,
            MermaidInlineTextParser.Parse(note.Text, isMarkdown: false));
    }
}

/// <summary>
/// Prepared group text and child hierarchy for a Flowchart/State graph.
/// </summary>
internal sealed record PreparedPositionedGroup(
    PositionedGroup Group,
    MermaidTextLayout LabelLayout,
    IReadOnlyList<PreparedPositionedGroup> Children);

/// <summary>
/// Prepared edge label and lazy rounded path for a Flowchart/State graph.
/// </summary>
/// <remarks>
/// The path is created lazily because <see cref="StreamGeometry.Open"/> requires Avalonia's platform
/// render services. Preparing the graph can then remain safe during headless measure/test paths while
/// the first real render still caches the geometry for subsequent frames.
/// </remarks>
internal sealed class PreparedPositionedEdge(
    PositionedEdge edge,
    double cornerRadius,
    Mermaider.Models.Point labelPosition,
    MermaidTextLayout? labelLayout)
{
    private StreamGeometry? _roundedPath;

    /// <summary>
    /// Original Mermaider edge model.
    /// </summary>
    public PositionedEdge Edge { get; } = edge;

    /// <summary>
    /// Label anchor resolved during preparation.
    /// </summary>
    public Mermaider.Models.Point LabelPosition { get; } = labelPosition;

    /// <summary>
    /// Parsed label text, when the edge has a label.
    /// </summary>
    public MermaidTextLayout? LabelLayout { get; } = labelLayout;

    /// <summary>
    /// Gets the cached rounded edge path, creating it on the first render-capable access.
    /// </summary>
    public StreamGeometry? RoundedPath =>
        Edge.Points.Count >= 2
            ? _roundedPath ??= MermaidDrawingHelpers.CreateRoundedPath(Edge.Points, cornerRadius)
            : null;
}

/// <summary>
/// Prepared node label for a Flowchart/State graph.
/// </summary>
internal sealed record PreparedPositionedNode(PositionedNode Node, MermaidTextLayout LabelLayout);

/// <summary>
/// Prepared note label for a Flowchart/State graph.
/// </summary>
internal sealed record PreparedPositionedNote(PositionedGraphNote Note, MermaidTextLayout TextLayout);

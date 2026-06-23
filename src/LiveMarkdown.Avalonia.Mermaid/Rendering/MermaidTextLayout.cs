namespace LiveMarkdown.Avalonia;

/// <summary>
/// Inline style flags used after Mermaid label text has been normalized into plain drawable text.
/// </summary>
/// <remarks>
/// Flags can be combined for nested or overlapping inline constructs. For example, parsing
/// <c>***important***</c> produces a span whose style is <see cref="Bold"/> | <see cref="Italic"/>.
/// </remarks>
[Flags]
internal enum MermaidTextStyle
{
    /// <summary>
    /// No additional inline style is applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Render the span using a bold font weight.
    /// </summary>
    Bold = 1 << 0,

    /// <summary>
    /// Render the span using an italic font style.
    /// </summary>
    Italic = 1 << 1,

    /// <summary>
    /// Render the span with underline decoration.
    /// </summary>
    Underline = 1 << 2,

    /// <summary>
    /// Render the span with strikethrough decoration.
    /// </summary>
    Strikethrough = 1 << 3,

    /// <summary>
    /// Mark the span as inline code. The current renderer maps this to a monospace typeface.
    /// </summary>
    Code = 1 << 4
}

/// <summary>
/// Describes a styled range in <see cref="MermaidTextLayout.Text"/>.
/// </summary>
/// <remarks>
/// <paramref name="Start"/> and <paramref name="Length"/> are offsets in the normalized output text,
/// not in the original Markdown or Mermaider HTML-like source. This keeps drawing independent from
/// parser-specific source spans.
/// </remarks>
internal readonly record struct MermaidTextSpan(int Start, int Length, MermaidTextStyle Style);

/// <summary>
/// Represents Mermaid label text after inline markup has been flattened for Avalonia text rendering.
/// </summary>
/// <remarks>
/// The renderer deliberately stores one plain text buffer plus style ranges instead of a tree. Mermaid
/// labels only need shallow inline formatting today, and this shape maps directly to
/// <c>FormattedText</c> range APIs.
/// </remarks>
internal sealed record MermaidTextLayout(string Text, IReadOnlyList<MermaidTextSpan> Spans)
{
    /// <summary>
    /// Creates a layout with no inline style ranges.
    /// </summary>
    public static MermaidTextLayout Plain(string? text) => new(text ?? string.Empty, []);
}
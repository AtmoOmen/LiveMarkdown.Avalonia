using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LiveMarkdown.Avalonia;

/// <summary>
/// A node that represents a container inline (e.g., Span).
/// </summary>
public class ContainerInlineNode() : InlinesNode(new global::Avalonia.Controls.Documents.Span())
{
    protected override bool IsCompatible(MarkdownObject markdownObject)
    {
        return markdownObject is ContainerInline and not EmphasisInline; // EmphasisInline is handled separately
    }
}